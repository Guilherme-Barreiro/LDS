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
    public class HorarioTrabalhoMedicoServiceTests
    {
        private static int SeedMedico(ApplicationDbContext db, string utente = "UT1")
        {
            var m = new Medico
            {
                NomeCompleto = "Dr Teste",
                Email = $"{utente}@mail.com",
                Telemovel = "900000000",
                NUtente = utente,
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-35)
            };
            db.Medicos.Add(m);
            db.SaveChanges();
            return m.Id;
        }

        [Fact]
        public async Task DefinirHorario_Cria_Valido()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            var id = await svc.DefinirHorarioAsync(medicoId, "Seg",
                TimeSpan.FromHours(9), TimeSpan.FromHours(12), CancellationToken.None);

            var h = db.HorariosTrabalhoMedicos.Single(x => x.Id == id);
            Assert.Equal("Seg", h.DiaSemana);
            Assert.Equal(TimeSpan.FromHours(9), h.HoraInicio);
            Assert.Equal(TimeSpan.FromHours(12), h.HoraFim);
            Assert.Equal(medicoId, h.MedicoId);
        }

        [Fact]
        public async Task DefinirHorario_MedicoInexistente_Lanca_KeyNotFound()
        {
            using var db = TestDb.Create();
            var svc = new HorarioTrabalhoMedicoService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.DefinirHorarioAsync(999, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(10), CancellationToken.None));
        }

        [Fact]
        public async Task DefinirHorario_DiaInvalido_Lanca_ArgumentException()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.DefinirHorarioAsync(medicoId, "ABC", TimeSpan.FromHours(9), TimeSpan.FromHours(10), CancellationToken.None));
        }

        [Fact]
        public async Task DefinirHorario_HorasInvalidas_Lanca_ArgumentException()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.DefinirHorarioAsync(medicoId, "Ter", TimeSpan.FromHours(12), TimeSpan.FromHours(12), CancellationToken.None));
        }

        [Fact]
        public async Task DefinirHorario_Sobreposicao_Lanca_InvalidOperation()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            await svc.DefinirHorarioAsync(medicoId, "Qua", TimeSpan.FromHours(9), TimeSpan.FromHours(12), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.DefinirHorarioAsync(medicoId, "Qua", TimeSpan.FromHours(11), TimeSpan.FromHours(13), CancellationToken.None));
        }

        [Fact]
        public async Task AtualizarHorario_Ok_AlteraValores()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            var id = await svc.DefinirHorarioAsync(medicoId, "Qui", TimeSpan.FromHours(9), TimeSpan.FromHours(11), CancellationToken.None);

            await svc.AtualizarHorarioAsync(medicoId, id, "Qui", TimeSpan.FromHours(8.5), TimeSpan.FromHours(11.5), CancellationToken.None);

            var h = db.HorariosTrabalhoMedicos.Single(x => x.Id == id);
            Assert.Equal(TimeSpan.FromHours(8.5), h.HoraInicio);
            Assert.Equal(TimeSpan.FromHours(11.5), h.HoraFim);
        }

        [Fact]
        public async Task AtualizarHorario_Inexistente_Lanca_KeyNotFound()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.AtualizarHorarioAsync(medicoId, 999, "Sex", TimeSpan.FromHours(9), TimeSpan.FromHours(10), CancellationToken.None));
        }

        [Fact]
        public async Task AtualizarHorario_DeOutroMedico_Lanca_UnauthorizedAccess()
        {
            using var db = TestDb.Create();
            var medicoA = SeedMedico(db, "UTA");
            var medicoB = SeedMedico(db, "UTB");
            var svc = new HorarioTrabalhoMedicoService(db);

            var id = await svc.DefinirHorarioAsync(medicoA, "Sex", TimeSpan.FromHours(9), TimeSpan.FromHours(11), CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.AtualizarHorarioAsync(medicoB, id, "Sex", TimeSpan.FromHours(10), TimeSpan.FromHours(12), CancellationToken.None));
        }

        [Fact]
        public async Task AtualizarHorario_CausaSobreposicao_Lanca_InvalidOperation()
        {
            using var db = TestDb.Create();
            var medicoId = SeedMedico(db);
            var svc = new HorarioTrabalhoMedicoService(db);

            var id1 = await svc.DefinirHorarioAsync(medicoId, "Dom", TimeSpan.FromHours(9), TimeSpan.FromHours(11), CancellationToken.None);
            var id2 = await svc.DefinirHorarioAsync(medicoId, "Dom", TimeSpan.FromHours(12), TimeSpan.FromHours(14), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AtualizarHorarioAsync(medicoId, id2, "Dom", TimeSpan.FromHours(10), TimeSpan.FromHours(13), CancellationToken.None));
        }
    }
}
