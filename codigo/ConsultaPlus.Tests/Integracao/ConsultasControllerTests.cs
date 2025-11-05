using System.Net;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Consultas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Tests.Integracao.Consultas
{
    [Collection("Integration")] // usa a mesma collection da tua ApiFactory
    public class ConsultasControllerIT : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public ConsultasControllerIT(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ----------------- helpers -----------------

        private sealed record Ids(int PacienteId, int MedicoId, int SalaId, int EspecialidadeId);

        private async Task<Ids> SeedBaseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // limpar para isolar
            db.Consultas.RemoveRange(db.Consultas);
            db.Medicos.RemoveRange(db.Medicos);
            db.Pacientes.RemoveRange(db.Pacientes);
            db.Salas.RemoveRange(db.Salas);
            db.Especialidades.RemoveRange(db.Especialidades);
            await db.SaveChangesAsync();

            var med = new ConsultaPlus.Core.Models.Medico
            {   
                NomeCompleto = "Dr IT",
                Email = "dr@it",
                Telemovel = "900000000",              // obrigatório
                NUtente = Guid.NewGuid().ToString("N")[..12],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-40)
            };
            var pac = new Paciente
            {
                NomeCompleto = "Pac IT",
                Email = "pac@it",
                Telemovel = "911111111",
                NUtente = Guid.NewGuid().ToString("N")[..12],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-20)
            };
            var esp = new ConsultaPlus.Core.Models.Especialidade { Nome = "Cardio" };
            var sala = new ConsultaPlus.Core.Models.Sala { Nome = "Sala 1" };

            db.Medicos.Add(med);
            db.Pacientes.Add(pac);
            db.Especialidades.Add(esp);
            db.Salas.Add(sala);
            await db.SaveChangesAsync();

            return new Ids(pac.Id, med.Id, sala.Id, esp.Id);
        }

        private async Task<ConsultaResponseDto> PostConsultaAsync(Ids ids, DateTime inicioUtc, int duracaoMin)
        {
            var body = new
            {
                pacienteId = ids.PacienteId,
                medicoId = ids.MedicoId,
                salaId = ids.SalaId,
                especialidadeId = ids.EspecialidadeId,
                dataConsulta = inicioUtc,
                duracao = duracaoMin
            };

            var resp = await _client.PostAsJsonAsync("/api/Consultas", body);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            var dto = await resp.Content.ReadFromJsonAsync<ConsultaResponseDto>();
            Assert.NotNull(dto);
            return dto!;
        }

        // ----------------- testes -----------------

        [Fact]
        public async Task Post__201_ComBody_EstadoConfirmada()
        {
            var ids = await SeedBaseAsync();
            var inicio = new DateTime(2025, 11, 04, 10, 0, 0, DateTimeKind.Utc);

            var created = await PostConsultaAsync(ids, inicio, 30);

            Assert.True(created.Id > 0);
            Assert.Equal(ids.PacienteId, created.PacienteId);
            Assert.Equal(ids.MedicoId, created.MedicoId);
            Assert.Equal(ids.SalaId, created.SalaId);
            Assert.Equal(ids.EspecialidadeId, created.EspecialidadeId);
            Assert.Equal(inicio, created.DataConsulta);
            Assert.Equal(30, created.Duracao);
            Assert.Equal("Confirmada", created.Estado);   // regra do controller
        }

        [Fact]
        public async Task GetById__200_AposCriar()
        {
            var ids = await SeedBaseAsync();
            var created = await PostConsultaAsync(ids, DateTime.UtcNow, 20);

            var resp = await _client.GetAsync($"/api/Consultas/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var dto = await resp.Content.ReadFromJsonAsync<ConsultaResponseDto>();
            Assert.NotNull(dto);
            Assert.Equal(created.Id, dto!.Id);
        }

        [Fact]
        public async Task GetAll__200_ContemRegistos()
        {
            var ids = await SeedBaseAsync();
            await PostConsultaAsync(ids, DateTime.UtcNow.AddHours(1), 30);
            await PostConsultaAsync(ids, DateTime.UtcNow.AddHours(2), 30);

            var resp = await _client.GetAsync("/api/Consultas");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<ConsultaResponseDto>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 2);
        }

        [Fact]
        public async Task GetByMedico__FiltraPorMedico()
        {
            var ids = await SeedBaseAsync();

            // cria para medico A (ids.MedicoId)
            await PostConsultaAsync(ids, DateTime.UtcNow.AddHours(1), 30);

            // cria para outro médico
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var outro = new ConsultaPlus.Core.Models.Medico
                {
                    NomeCompleto = "Dr B",
                    Email = "b@x",
                    Telemovel = "900000001",
                    NUtente = Guid.NewGuid().ToString("N")[..12],
                    PasswordHash = "x",
                    DataNascimento = DateTime.UtcNow.AddYears(-45)
                };
                db.Medicos.Add(outro);
                await db.SaveChangesAsync();

                db.Consultas.Add(new Consulta
                {
                    PacienteId = ids.PacienteId,
                    MedicoId = outro.Id,
                    SalaId = ids.SalaId,
                    EspecialidadeId = ids.EspecialidadeId,
                    DataConsulta = DateTime.UtcNow.AddHours(2),
                    Duracao = 30,
                    Estado = "Confirmada"
                });
                await db.SaveChangesAsync();
            }

            var resp = await _client.GetAsync($"/api/Consultas/medico/{ids.MedicoId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<ConsultaResponseDto>>();
            Assert.NotNull(list);
            Assert.All(list!, c => Assert.Equal(ids.MedicoId, c.MedicoId));
        }

        [Fact]
        public async Task GetByPaciente__FiltraPorPaciente()
        {
            var ids = await SeedBaseAsync();

            // cria 2 para o mesmo paciente
            await PostConsultaAsync(ids, DateTime.UtcNow.AddHours(1), 30);
            await PostConsultaAsync(ids, DateTime.UtcNow.AddHours(2), 30);

            // cria 1 para outro paciente
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var outro = new Paciente
                {
                    NomeCompleto = "Pac B",
                    Email = "pb@x",
                    Telemovel = "922222222",
                    NUtente = Guid.NewGuid().ToString("N")[..12],
                    PasswordHash = "x",
                    DataNascimento = DateTime.UtcNow.AddYears(-30)
                };
                db.Pacientes.Add(outro);
                await db.SaveChangesAsync();

                db.Consultas.Add(new Consulta
                {
                    PacienteId = outro.Id,
                    MedicoId = ids.MedicoId,
                    SalaId = ids.SalaId,
                    EspecialidadeId = ids.EspecialidadeId,
                    DataConsulta = DateTime.UtcNow.AddHours(3),
                    Duracao = 30,
                    Estado = "Confirmada"
                });
                await db.SaveChangesAsync();
            }

            var resp = await _client.GetAsync($"/api/Consultas/paciente/{ids.PacienteId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<ConsultaResponseDto>>();
            Assert.NotNull(list);
            Assert.All(list!, c => Assert.Equal(ids.PacienteId, c.PacienteId));
        }

        [Fact]
        public async Task Delete__204_E_AposIsso_GetById_404()
        {
            var ids = await SeedBaseAsync();
            var created = await PostConsultaAsync(ids, DateTime.UtcNow.AddHours(1), 30);

            var del = await _client.DeleteAsync($"/api/Consultas/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var nf = await _client.GetAsync($"/api/Consultas/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
        }
    }
}
