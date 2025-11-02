using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ConsultaPlus.Tests.HorarioMedico
{
    public class AdminHorariosControllerTests
    {
        private static AdminHorariosController SUT(
            ApplicationDbContext db,
            Mock<IHorarioTrabalhoMedico>? hMock = null,
            Mock<IHorarioExcecaoMedico>? eMock = null)
        {
            hMock ??= new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            eMock ??= new Mock<IHorarioExcecaoMedico>(MockBehavior.Strict);
            return new AdminHorariosController(hMock.Object, eMock.Object, db);
        }

        [Fact]
        public async Task GetHorarios_FiltraPorMedico_Ordena_RetornaDois()
        {
            // BD isolada por teste + limpa
            using var db = ConsultaPlus.Tests.HorarioMedico.TestDb.Create();
            db.HorariosTrabalhoMedicos.RemoveRange(db.HorariosTrabalhoMedicos);
            await db.SaveChangesAsync();

            var medicoId = 77;

            // Semeia 2 horários do médico 77
            // antes: "Qua" fazia a ordem ficar "Qua" -> "Seg"
            db.HorariosTrabalhoMedicos.AddRange(
                new HorarioTrabalhoMedico
                {
                    MedicoId = medicoId,
                    DiaSemana = "Ter",                 // <= trocado de "Qua" para "Ter"
                    HoraInicio = TimeSpan.FromHours(8),
                    HoraFim = TimeSpan.FromHours(9)
                },
                new HorarioTrabalhoMedico
                {
                    MedicoId = medicoId,
                    DiaSemana = "Seg",
                    HoraInicio = TimeSpan.FromHours(9),
                    HoraFim = TimeSpan.FromHours(12)
                }
            );

            // ruído: outro médico
            db.HorariosTrabalhoMedicos.Add(new ConsultaPlus.Core.Models.HorarioTrabalhoMedico
            {
                MedicoId = 99,
                DiaSemana = "Seg",
                HoraInicio = TimeSpan.FromHours(7),
                HoraFim = TimeSpan.FromHours(8)
            });

            await db.SaveChangesAsync();

            var ctrl = new ConsultaPlus.API.Controllers.AdminHorariosController(
                new Mock<ConsultaPlus.Core.Interfaces.IHorarioTrabalhoMedico>().Object,
                new Mock<ConsultaPlus.Core.Interfaces.IHorarioExcecaoMedico>().Object,
                db
            );

            // Act
            var res = await ctrl.GetHorarios(medicoId, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(res);

            // Converte o value (IEnumerable de tipo anónimo) para lista de object
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Equal(2, list.Count);

            // Helpers para ler propriedades do tipo anónimo
            static string GetString(object o, string prop)
                => (string)o.GetType().GetProperty(prop)!.GetValue(o)!;

            static TimeSpan GetTime(object o, string prop)
                => (TimeSpan)o.GetType().GetProperty(prop)!.GetValue(o)!;

            // Primeiro item (ordenado por DiaSemana, depois HoraInicio)
            var first = list[0];
            Assert.Equal("Seg", GetString(first, "DiaSemana"));
            Assert.Equal(TimeSpan.FromHours(9), GetTime(first, "HoraInicio"));

            // Segundo item
            var second = list[1];
            Assert.Equal("Ter", GetString(second, "DiaSemana"));
            Assert.Equal(TimeSpan.FromHours(8), GetTime(second, "HoraInicio"));

        }

        [Fact]
        public async Task AtualizarHorario_Sucesso_DevolveOk_ComRegistoAtualizado()
        {
            using var db = TestDb.Create();
            var h = new HorarioTrabalhoMedico { Id = 7, MedicoId = 10, DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9) };
            db.HorariosTrabalhoMedicos.Add(h);
            await db.SaveChangesAsync();

            var hSvc = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);

            hSvc.Setup(s => s.AtualizarHorarioAsync(10, 7, "Ter", TimeSpan.FromHours(10), TimeSpan.FromHours(12), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    h.DiaSemana = "Ter";
                    h.HoraInicio = TimeSpan.FromHours(10);
                    h.HoraFim = TimeSpan.FromHours(12);
                    db.SaveChanges();
                })
                .Returns(Task.CompletedTask);

            var sut = SUT(db, hMock: hSvc);
            var req = new AtualizarHorarioRequest { DiaSemana = "ter", HoraInicio = TimeSpan.FromHours(10), HoraFim = TimeSpan.FromHours(12) };

            var result = await sut.AtualizarHorario(10, 7, req, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;
            T Get<T> (string name) => (T)body.GetType().GetProperty(name)!.GetValue(body)!;
            Assert.Equal(7, Get<int>("Id"));
            Assert.Equal(10, Get<int>("MedicoId"));
            Assert.Equal("Ter", Get<string>("DiaSemana"));
            Assert.Equal(TimeSpan.FromHours(10), Get<TimeSpan>("HoraInicio"));
            Assert.Equal(TimeSpan.FromHours(12), Get<TimeSpan>("HoraFim"));

            hSvc.VerifyAll();
        }

        [Fact]
        public async Task GetExcecoes_SemFiltro_DevolveTodos_Ordenados()
        {
            using var db = TestDb.Create();
            db.HorariosExcecaoMedicos.AddRange(
                new HorarioExcecaoMedico { Id = 1, MedicoId = 5, Data = new DateTime(2025, 10, 28), HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10), Motivo = "B" },
                new HorarioExcecaoMedico { Id = 2, MedicoId = 5, Data = new DateTime(2025, 10, 27), HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9), Motivo = "A" },
                new HorarioExcecaoMedico { Id = 3, MedicoId = 99, Data = new DateTime(2025, 10, 27), HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9), Motivo = "X" }
            );
            await db.SaveChangesAsync();

            var sut = SUT(db);
            var result = await sut.GetExcecoes(5, data: null, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.List<ExcecaoDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.Equal(2, list[0].Id);
            Assert.Equal(1, list[1].Id);
        }

        [Fact]
        public async Task GetExcecoes_ComFiltroData_DevolveApenasDoDia()
        {
            using var db = TestDb.Create();
            db.HorariosExcecaoMedicos.AddRange(
                new HorarioExcecaoMedico { Id = 1, MedicoId = 5, Data = new DateTime(2025, 10, 27), HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10) },
                new HorarioExcecaoMedico { Id = 2, MedicoId = 5, Data = new DateTime(2025, 10, 28), HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10) }
            );
            await db.SaveChangesAsync();

            var sut = SUT(db);
            var result = await sut.GetExcecoes(5, new DateOnly(2025, 10, 27), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.List<ExcecaoDto>>(ok.Value);
            Assert.Single(list);
            Assert.Equal(1, list[0].Id);
        }

        [Fact]
        public async Task RemoverExcecao_NotFound_404()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);
            var result = await sut.RemoverExcecao(5, 999, CancellationToken.None);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RemoverExcecao_Sucesso_204()
        {
            using var db = TestDb.Create();
            db.HorariosExcecaoMedicos.Add(new HorarioExcecaoMedico { Id = 10, MedicoId = 5, Data = DateTime.UtcNow, HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10) });
            await db.SaveChangesAsync();

            var sut = SUT(db);
            var result = await sut.RemoverExcecao(5, 10, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
            Assert.False(db.HorariosExcecaoMedicos.Any(e => e.Id == 10));
        }
    }
}
