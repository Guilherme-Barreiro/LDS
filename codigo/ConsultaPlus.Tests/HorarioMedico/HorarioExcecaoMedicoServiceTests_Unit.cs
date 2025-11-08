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
    public class HorarioExcecaoMedicoServiceTests_Unit
    {
        private static Mock<ApplicationDbContext> MockCtx(
    out List<ConsultaPlus.Core.Models.Medico> medicos,
    out List<ConsultaPlus.Core.Models.HorarioExcecaoMedico> excecoes)
        {
            var medicosList = new List<ConsultaPlus.Core.Models.Medico>();
            var excecoesList = new List<ConsultaPlus.Core.Models.HorarioExcecaoMedico>();

            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                       .UseInMemoryDatabase(Guid.NewGuid().ToString())
                       .Options;

            var ctx = new Mock<ApplicationDbContext>(opts) { CallBase = false };

            ctx.SetupGet(c => c.Medicos).ReturnsDbSet(medicosList);

            var excecoesDbSetMock = new Mock<DbSet<ConsultaPlus.Core.Models.HorarioExcecaoMedico>>();

            excecoesDbSetMock
                .Setup(m => m.Add(It.IsAny<ConsultaPlus.Core.Models.HorarioExcecaoMedico>()))
                .Callback<ConsultaPlus.Core.Models.HorarioExcecaoMedico>(e => excecoesList.Add(e));

            excecoesDbSetMock
                .Setup(m => m.AddRange(It.IsAny<IEnumerable<ConsultaPlus.Core.Models.HorarioExcecaoMedico>>()))
                .Callback<IEnumerable<ConsultaPlus.Core.Models.HorarioExcecaoMedico>>(es => excecoesList.AddRange(es));

            excecoesDbSetMock
                .Setup(m => m.Remove(It.IsAny<ConsultaPlus.Core.Models.HorarioExcecaoMedico>()))
                .Callback<ConsultaPlus.Core.Models.HorarioExcecaoMedico>(e => excecoesList.Remove(e));

            ctx.SetupGet(c => c.HorariosExcecaoMedicos)
               .ReturnsDbSet(excecoesList, excecoesDbSetMock);

            ctx.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);

            medicos = medicosList;
            excecoes = excecoesList;

            return ctx;
        }

        private static int AddMedico(List<ConsultaPlus.Core.Models.Medico> medicos, string utente = "UTX")
        {
            var id = (medicos.LastOrDefault()?.Id ?? 0) + 1;
            medicos.Add(new ConsultaPlus.Core.Models.Medico
            {
                Id = id,
                NomeCompleto = "Dr Ex",
                Email = $"{utente}@mail.com",
                Telemovel = "900000001",
                NUtente = utente,
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-33)
            });
            return id;
        }

        [Fact]
        public async Task RegistarExcecao_Valida_Grava()
        {
            var ctx = MockCtx(out var medicos, out var excecoes);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioExcecaoMedicoService(ctx.Object);

            await svc.RegistarExcecaoAsync(medicoId, new DateOnly(2025, 10, 27),
                TimeSpan.FromHours(10), TimeSpan.FromHours(12), true, "Formação", CancellationToken.None);

            ctx.Verify(c => c.HorariosExcecaoMedicos.Add(
                It.Is<ConsultaPlus.Core.Models.HorarioExcecaoMedico>(e =>
                    e.MedicoId == medicoId &&
                    e.IsReducao == true &&
                    e.HoraInicio == TimeSpan.FromHours(10) &&
                    e.HoraFim == TimeSpan.FromHours(12)
                )), Times.Once);

            ctx.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegistarExcecao_DuplicadoExato_Ignora()
        {
            var ctx = MockCtx(out var medicos, out var excecoes);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioExcecaoMedicoService(ctx.Object);
            var d = new DateOnly(2025, 10, 28);

            await svc.RegistarExcecaoAsync(medicoId, d, TimeSpan.FromHours(9), TimeSpan.FromHours(10), true, "Reunião", CancellationToken.None);
            await svc.RegistarExcecaoAsync(medicoId, d, TimeSpan.FromHours(9), TimeSpan.FromHours(10), true, "Reunião", CancellationToken.None);

            ctx.Verify(c => c.HorariosExcecaoMedicos.Add(It.IsAny<ConsultaPlus.Core.Models.HorarioExcecaoMedico>()), Times.Once);
        }

        [Fact]
        public async Task RegistarExcecao_HorasInvalidas_Lanca_ArgumentException()
        {
            var ctx = MockCtx(out var medicos, out _);
            var medicoId = AddMedico(medicos);
            var svc = new HorarioExcecaoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.RegistarExcecaoAsync(medicoId, new DateOnly(2025, 11, 1),
                    TimeSpan.FromHours(12), TimeSpan.FromHours(11), false, null, CancellationToken.None));
        }

        [Fact]
        public async Task RegistarExcecao_MedicoInexistente_Lanca_KeyNotFound()
        {
            var ctx = MockCtx(out _, out _);
            var svc = new HorarioExcecaoMedicoService(ctx.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.RegistarExcecaoAsync(9999, new DateOnly(2025, 11, 2),
                    TimeSpan.FromHours(9), TimeSpan.FromHours(10), false, null, CancellationToken.None));
        }
    }
}
