using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;
using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Infrastructure.Services;

namespace ConsultaPlus.Tests.HorarioMedico
{
    public class HorarioTrabalhoMedicoServiceTests_Unit
    {
        private static Mock<ApplicationDbContext> MockCtx(
            out List<ConsultaPlus.Core.Models.Medico> medicos,
            out List<ConsultaPlus.Core.Models.HorarioTrabalhoMedico> horarios)
        {
            medicos = new();
            horarios = new();

            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                       .UseInMemoryDatabase(Guid.NewGuid().ToString())
                       .Options;

            var ctx = new Mock<ApplicationDbContext>(opts) { CallBase = false };

            ctx.SetupGet(c => c.Medicos).ReturnsDbSet(medicos);
            ctx.SetupGet(c => c.HorariosTrabalhoMedicos).ReturnsDbSet(horarios);

            ctx.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);

            return ctx;
        }

        private static int AddMedico(List<ConsultaPlus.Core.Models.Medico> medicos, string utente = "UT1")
        {
            var id = (medicos.LastOrDefault()?.Id ?? 0) + 1;
            medicos.Add(new ConsultaPlus.Core.Models.Medico
            {
                Id = id,
                NomeCompleto = "Dr Teste",
                Email = $"{utente}@mail.com",
                Telemovel = "900000000",
                NUtente = utente,
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-35)
            });
            return id;
        }

        [Fact]
        public async Task DefinirHorario_Cria_Valido()
        {
            var ctx = MockCtx(out var medicos, out var horarios);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            _ = await svc.DefinirHorarioAsync(medicoId, "Seg",
                TimeSpan.FromHours(9), TimeSpan.FromHours(12), CancellationToken.None);

            ctx.Verify(c => c.HorariosTrabalhoMedicos.Add(
            It.Is<ConsultaPlus.Core.Models.HorarioTrabalhoMedico>(h =>
            h.DiaSemana == "Seg" &&
            h.HoraInicio == TimeSpan.FromHours(9) &&
            h.HoraFim == TimeSpan.FromHours(12) &&
            h.MedicoId == medicoId
            )), Times.Once);

            ctx.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        public async Task DefinirHorario_MedicoInexistente_Lanca_KeyNotFound()
        {
            var ctx = MockCtx(out _, out _);
            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.DefinirHorarioAsync(999, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(10), CancellationToken.None));
        }

        [Fact]
        public async Task DefinirHorario_DiaInvalido_Lanca_ArgumentException()
        {
            var ctx = MockCtx(out var medicos, out _);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.DefinirHorarioAsync(medicoId, "ABC", TimeSpan.FromHours(9), TimeSpan.FromHours(10), CancellationToken.None));
        }

        [Fact]
        public async Task DefinirHorario_HorasInvalidas_Lanca_ArgumentException()
        {
            var ctx = MockCtx(out var medicos, out _);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.DefinirHorarioAsync(medicoId, "Ter", TimeSpan.FromHours(12), TimeSpan.FromHours(12), CancellationToken.None));
        }

        [Fact]
        public async Task DefinirHorario_Sobreposicao_Lanca_InvalidOperation()
        {
            var ctx = MockCtx(out var medicos, out var horarios);
            var medicoId = AddMedico(medicos);
            horarios.Add(new ConsultaPlus.Core.Models.HorarioTrabalhoMedico
            {
                Id = 1,
                MedicoId = medicoId,
                DiaSemana = "Qua",
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(12)
            });

            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.DefinirHorarioAsync(medicoId, "Qua", TimeSpan.FromHours(11), TimeSpan.FromHours(13), CancellationToken.None));
        }

        [Fact]
        public async Task AtualizarHorario_Ok_AlteraValores()
        {
            var ctx = MockCtx(out var medicos, out var horarios);
            var medicoId = AddMedico(medicos);
            horarios.Add(new ConsultaPlus.Core.Models.HorarioTrabalhoMedico
            {
                Id = 10,
                MedicoId = medicoId,
                DiaSemana = "Qui",
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(11)
            });

            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await svc.AtualizarHorarioAsync(medicoId, 10, "Qui",
                TimeSpan.FromHours(8.5), TimeSpan.FromHours(11.5), CancellationToken.None);

            var h = horarios.Single(x => x.Id == 10);
            Assert.Equal(TimeSpan.FromHours(8.5), h.HoraInicio);
            Assert.Equal(TimeSpan.FromHours(11.5), h.HoraFim);
            ctx.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AtualizarHorario_Inexistente_Lanca_KeyNotFound()
        {
            var ctx = MockCtx(out var medicos, out _);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.AtualizarHorarioAsync(medicoId, 999, "Sex", TimeSpan.FromHours(9), TimeSpan.FromHours(10), CancellationToken.None));
        }

        [Fact]
        public async Task AtualizarHorario_DeOutroMedico_Lanca_UnauthorizedAccess()
        {
            var ctx = MockCtx(out var medicos, out var horarios);
            var medicoA = AddMedico(medicos, "UTA");
            var medicoB = AddMedico(medicos, "UTB");
            horarios.Add(new ConsultaPlus.Core.Models.HorarioTrabalhoMedico
            {
                Id = 20,
                MedicoId = medicoA,
                DiaSemana = "Sex",
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(11)
            });

            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.AtualizarHorarioAsync(medicoB, 20, "Sex", TimeSpan.FromHours(10), TimeSpan.FromHours(12), CancellationToken.None));
        }

        [Fact]
        public async Task AtualizarHorario_CausaSobreposicao_Lanca_InvalidOperation()
        {
            var ctx = MockCtx(out var medicos, out var horarios);
            var medicoId = AddMedico(medicos);
            horarios.AddRange(new[]
            {
                new ConsultaPlus.Core.Models.HorarioTrabalhoMedico { Id = 30, MedicoId = medicoId, DiaSemana = "Dom", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(11) },
                new ConsultaPlus.Core.Models.HorarioTrabalhoMedico { Id = 31, MedicoId = medicoId, DiaSemana = "Dom", HoraInicio = TimeSpan.FromHours(12), HoraFim = TimeSpan.FromHours(14) }
            });

            var svc = new HorarioTrabalhoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AtualizarHorarioAsync(medicoId, 31, "Dom", TimeSpan.FromHours(10), TimeSpan.FromHours(13), CancellationToken.None));
        }
    }
}
