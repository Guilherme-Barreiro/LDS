using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Relatorios
{
    public class RelatorioControllerTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public RelatorioControllerTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(); 
        }

        private ApplicationDbContext GetDb()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        private async Task CleanupAsync()
        {
            using var db = GetDb();
            db.Consultas.RemoveRange(db.Consultas);
            db.Medicos.RemoveRange(db.Medicos);
            db.Pacientes.RemoveRange(db.Pacientes);
            db.Especialidades.RemoveRange(db.Especialidades);
            db.Salas.RemoveRange(db.Salas);
            await db.SaveChangesAsync();
        }

        private async Task<(int medicoId, int pacienteId, int especialidadeId, int salaId)> SeedBaseAsync()
        {
            using var db = GetDb();

            var med = new Core.Models.Medico
            {
                NomeCompleto = "Dr E2E",
                Email = "e2e@ex.com",
                Telemovel = "900000000",
                NUtente = Guid.NewGuid().ToString("N")[..10],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-40)
            };
            var pac = new Paciente
            {
                NomeCompleto = "Pac E2E",
                Email = "pac@ex.com",
                Telemovel = "911111111",
                NUtente = Guid.NewGuid().ToString("N")[..10],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-20)
            };
            var esp = new Core.Models.Especialidade { Nome = "Cardiologia" };
            var sala = new Core.Models.Sala { Nome = "Sala A" };

            db.Medicos.Add(med);
            db.Pacientes.Add(pac);
            db.Especialidades.Add(esp);
            db.Salas.Add(sala);
            await db.SaveChangesAsync();

            return (med.Id, pac.Id, esp.Id, sala.Id);
        }

        private async Task SeedConsultasAsync(
            (int medicoId, int pacienteId, int especialidadeId, int salaId) ids,
            DateTime inicio,
            DateTime fim)
        {
            using var db = GetDb();

            db.Consultas.AddRange(
                new Consulta
                {
                    PacienteId = ids.pacienteId,
                    MedicoId = ids.medicoId,
                    SalaId = ids.salaId,
                    EspecialidadeId = ids.especialidadeId,
                    DataConsulta = inicio.AddDays(1),
                    Duracao = 30,
                    Estado = "Confirmada"
                },
                new Consulta
                {
                    PacienteId = ids.pacienteId,
                    MedicoId = ids.medicoId,
                    SalaId = ids.salaId,
                    EspecialidadeId = ids.especialidadeId,
                    DataConsulta = inicio.AddDays(5),
                    Duracao = 60,
                    Estado = "Confirmada"
                }
            );

            db.Consultas.Add(
                new Consulta
                {
                    PacienteId = ids.pacienteId,
                    MedicoId = ids.medicoId,
                    SalaId = ids.salaId,
                    EspecialidadeId = ids.especialidadeId,
                    DataConsulta = fim.AddDays(10),
                    Duracao = 30,
                    Estado = "Confirmada"
                });

            db.Consultas.Add(
                new Consulta
                {
                    PacienteId = ids.pacienteId,
                    MedicoId = ids.medicoId,
                    SalaId = ids.salaId,
                    EspecialidadeId = ids.especialidadeId,
                    DataConsulta = inicio.AddDays(2),
                    Duracao = 30,
                    Estado = "NaoCompareceu" 
                });

            await db.SaveChangesAsync();
        }


        [Fact]
        public async Task ConsultasPorPeriodo__200_ComIntervaloValido_E_ComMedicoOpcional()
        {
            await CleanupAsync();
            var ids = await SeedBaseAsync();

            var di = new DateTime(2025, 01, 01);
            var df = new DateTime(2025, 01, 31);
            await SeedConsultasAsync(ids, di, df);

            var url1 = $"/api/Relatorio/consultas-por-periodo?DataInicio={di:yyyy-MM-dd}&DataFim={df:yyyy-MM-dd}";
            var r1 = await _client.GetAsync(url1);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            var json1 = await r1.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(json1));

            var url2 = $"/api/Relatorio/consultas-por-periodo?DataInicio={di:yyyy-MM-dd}&DataFim={df:yyyy-MM-dd}&MedicoId={ids.medicoId}";
            var r2 = await _client.GetAsync(url2);
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            var json2 = await r2.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(json2));
        }

        [Fact]
        public async Task ConsultasPorPeriodo__400_QuandoDataInicioMaiorQueFim()
        {
            await CleanupAsync();

            var r = await _client.GetAsync("/api/Relatorio/consultas-por-periodo?DataInicio=2025-02-01&DataFim=2025-01-01");
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }

        [Fact]
        public async Task TaxaNaoComparecimento__200_ComPeriodoMenorQueUmAno_E_ComFiltrosOpcionais()
        {
            await CleanupAsync();
            var ids = await SeedBaseAsync();

            var di = new DateTime(2025, 03, 01);
            var df = new DateTime(2025, 04, 01);
            await SeedConsultasAsync(ids, di, df);

            var url1 = $"/api/Relatorio/taxa-nao-comparecimento?DataInicio={di:yyyy-MM-dd}&DataFim={df:yyyy-MM-dd}";
            var r1 = await _client.GetAsync(url1);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            var json1 = await r1.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(json1));

            var url2 = $"/api/Relatorio/taxa-nao-comparecimento?DataInicio={di:yyyy-MM-dd}&DataFim={df:yyyy-MM-dd}&MedicoId={ids.medicoId}&EspecialidadeId={ids.especialidadeId}";
            var r2 = await _client.GetAsync(url2);
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            var json2 = await r2.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(json2));
        }

        [Fact]
        public async Task TaxaNaoComparecimento__400_QuandoPeriodoExcedeUmAno()
        {
            await CleanupAsync();

            var r = await _client.GetAsync("/api/Relatorio/taxa-nao-comparecimento?DataInicio=2024-01-01&DataFim=2025-07-01");
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
    }
}
