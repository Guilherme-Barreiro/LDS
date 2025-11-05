using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.API.DTOs.Consultas;

namespace ConsultaPlus.Tests.Integracao.Dashboard
{
    // Evita interferências entre testes (partilham a mesma app)
    [CollectionDefinition("Integration", DisableParallelization = true)]
    public class IntegrationCollection : ICollectionFixture<ApiFactory> { }

    [Collection("Integration")]
    public class DashboardControllerTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public DashboardControllerTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ---------- helpers ----------

        private async Task<(int MedicoId, int PacienteId)> SeedBasicoAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // limpar tudo o que interessa (para isolar testes)
            db.Consultas.RemoveRange(db.Consultas);
            db.Medicos.RemoveRange(db.Medicos);
            db.Pacientes.RemoveRange(db.Pacientes);
            db.Especialidades.RemoveRange(db.Especialidades);
            db.Salas.RemoveRange(db.Salas);
            await db.SaveChangesAsync();

            var med = new ConsultaPlus.Core.Models.Medico { NomeCompleto = "Dr IT", Email = "dr@it", Telemovel = "900000000", NUtente = Guid.NewGuid().ToString("N")[..12], PasswordHash = "x", DataNascimento = DateTime.UtcNow.AddYears(-40) };
            var pac = new Paciente { NomeCompleto = "Pac IT", Email = "pac@it", NUtente = Guid.NewGuid().ToString("N")[..12], PasswordHash = "x", DataNascimento = DateTime.UtcNow.AddYears(-20) };
            var esp = new ConsultaPlus.Core.Models.Especialidade { Nome = "Cardio" };
            var sala = new ConsultaPlus.Core.Models.Sala { Nome = "Sala 1" };

            db.Medicos.Add(med);
            db.Pacientes.Add(pac);
            db.Especialidades.Add(esp);
            db.Salas.Add(sala);
            await db.SaveChangesAsync();

            // 2 consultas no dia 2025-11-04 e 1 fora do dia
            db.Consultas.AddRange(
                new Consulta
                {
                    PacienteId = pac.Id,
                    MedicoId = med.Id,
                    EspecialidadeId = esp.Id,
                    SalaId = sala.Id,
                    DataConsulta = new DateTime(2025, 11, 04, 10, 00, 00, DateTimeKind.Utc),
                    Duracao = 30,
                    Estado = "Confirmada"
                },
                new Consulta
                {
                    PacienteId = pac.Id,
                    MedicoId = med.Id,
                    EspecialidadeId = esp.Id,
                    SalaId = sala.Id,
                    DataConsulta = new DateTime(2025, 11, 04, 15, 00, 00, DateTimeKind.Utc),
                    Duracao = 60,
                    Estado = "Pendente"
                },
                new Consulta // fora do intervalo 04..04
                {
                    PacienteId = pac.Id,
                    MedicoId = med.Id,
                    EspecialidadeId = esp.Id,
                    SalaId = sala.Id,
                    DataConsulta = new DateTime(2025, 11, 06, 09, 00, 00, DateTimeKind.Utc),
                    Duracao = 30,
                    Estado = "Confirmada"
                }
            );
            await db.SaveChangesAsync();

            return (med.Id, pac.Id);
        }

        // ---------- testes ----------

        [Fact]
        public async Task AgendaMedico__Devolve_DiaInteiro_Usando_ToExclusivo()
        {
            var (medicoId, _) = await SeedBasicoAsync();

            // 'to=2025-11-04' => controller converte para exclusivo 2025-11-05
            var resp = await _client.GetAsync($"/api/Dashboard/medico/{medicoId}/consultas?from=2025-11-04&to=2025-11-04&onlyConfirmed=false");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var items = await resp.Content.ReadFromJsonAsync<List<AgendaItemDto>>();
            Assert.NotNull(items);
            Assert.Equal(2, items!.Count);
            Assert.All(items, i => Assert.Equal(new DateTime(2025, 11, 04), i.Inicio.Date));
        }

        [Fact]
        public async Task AgendaMedico__BadRequest_Quando_ToMenorQueFrom()
        {
            var (medicoId, _) = await SeedBasicoAsync();

            var resp = await _client.GetAsync($"/api/Dashboard/medico/{medicoId}/consultas?from=2025-11-05&to=2025-11-04");

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task HistoricoPaciente__Paginado_Devolve_TotalEItems()
        {
            var (_, pacienteId) = await SeedBasicoAsync();

            var resp = await _client.GetAsync($"/api/Dashboard/paciente/{pacienteId}/consultas?page=1&pageSize=10");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var page = await resp.Content.ReadFromJsonAsync<PagedListDto<ConsultaPacienteDto>>();
            Assert.NotNull(page);
            Assert.Equal(1, page!.Page);
            Assert.Equal(10, page.PageSize);
            Assert.True(page.Total >= page.Items.Count);
            Assert.True(page.Items.Count >= 2);
        }
    }
}
