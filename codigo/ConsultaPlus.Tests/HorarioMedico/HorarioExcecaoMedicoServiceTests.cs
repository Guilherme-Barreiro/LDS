using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Infrastructure.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ConsultaPlus.Tests.HorarioMedico
{
    public class HorarioExcecaoMedicoServiceTests
    {
        private static int SeedMedico(ApplicationDbContext db, string utente = "UTX")
        {
            var m = new Medico
            {
                NomeCompleto = "Dr Ex",
                Email = $"{utente}@mail.com",
                Telemovel = "900000001",
                NUtente = utente,
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-33)
            };
            db.Medicos.Add(m);
            db.SaveChanges();
            return m.Id;
        }

        [Fact]
        public async Task RegistarExcecao_Valida_Grava()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioExcecaoMedicoService(db);

            await svc.RegistarExcecaoAsync(medicoId, new DateOnly(2025, 10, 27),
                TimeSpan.FromHours(10), TimeSpan.FromHours(12), true, "Formação", CancellationToken.None);

            var e = db.HorariosExcecaoMedicos.Single();
            Assert.Equal(medicoId, e.MedicoId);
            Assert.True(e.IsReducao);
            Assert.Equal(TimeSpan.FromHours(10), e.HoraInicio);
            Assert.Equal(TimeSpan.FromHours(12), e.HoraFim);
        }

        [Fact]
        public async Task RegistarExcecao_DuplicadoExato_Ignora()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioExcecaoMedicoService(db);
            var d = new DateOnly(2025, 10, 28);

            await svc.RegistarExcecaoAsync(medicoId, d, TimeSpan.FromHours(9), TimeSpan.FromHours(10), true, "Reunião", CancellationToken.None);
            await svc.RegistarExcecaoAsync(medicoId, d, TimeSpan.FromHours(9), TimeSpan.FromHours(10), true, "Reunião", CancellationToken.None);

            Assert.Single(db.HorariosExcecaoMedicos);
        }

        [Fact]
        public async Task RegistarExcecao_HorasInvalidas_Lanca_ArgumentException()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioExcecaoMedicoService(db);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.RegistarExcecaoAsync(medicoId, new DateOnly(2025, 11, 1),
                    TimeSpan.FromHours(12), TimeSpan.FromHours(11), false, null, CancellationToken.None));
        }

        [Fact]
        public async Task RegistarExcecao_MedicoInexistente_Lanca_KeyNotFound()
        {
            using var db = TestDb.Create();
            var svc = new HorarioExcecaoMedicoService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.RegistarExcecaoAsync(9999, new DateOnly(2025, 11, 2),
                    TimeSpan.FromHours(9), TimeSpan.FromHours(10), false, null, CancellationToken.None));
        }
    }
}
