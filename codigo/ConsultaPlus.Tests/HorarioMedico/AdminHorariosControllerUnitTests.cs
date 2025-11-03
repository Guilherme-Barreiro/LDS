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

        // --------------------------------------------------------------------
        //     >>>>>>  OS TEUS TESTES ORIGINAIS (mantidos)  <<<<<<
        // --------------------------------------------------------------------
        [Fact]
        public async Task GetHorarios_FiltraPorMedico_Ordena_RetornaDois()
        {
            using var db = TestDb.Create();
            db.HorariosTrabalhoMedicos.RemoveRange(db.HorariosTrabalhoMedicos);
            await db.SaveChangesAsync();

            var medicoId = 77;

            db.HorariosTrabalhoMedicos.AddRange(
                new HorarioTrabalhoMedico
                {
                    MedicoId = medicoId,
                    DiaSemana = "Ter",
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

            db.HorariosTrabalhoMedicos.Add(new HorarioTrabalhoMedico
            {
                MedicoId = 99,
                DiaSemana = "Seg",
                HoraInicio = TimeSpan.FromHours(7),
                HoraFim = TimeSpan.FromHours(8)
            });

            await db.SaveChangesAsync();

            var ctrl = new AdminHorariosController(
                new Mock<IHorarioTrabalhoMedico>().Object,
                new Mock<IHorarioExcecaoMedico>().Object,
                db
            );

            var res = await ctrl.GetHorarios(medicoId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(res);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Equal(2, list.Count);

            static string GetString(object o, string prop)
                => (string)o.GetType().GetProperty(prop)!.GetValue(o)!;

            static TimeSpan GetTime(object o, string prop)
                => (TimeSpan)o.GetType().GetProperty(prop)!.GetValue(o)!;

            var first = list[0];
            Assert.Equal("Seg", GetString(first, "DiaSemana"));
            Assert.Equal(TimeSpan.FromHours(9), GetTime(first, "HoraInicio"));

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
            T Get<T>(string name) => (T)body.GetType().GetProperty(name)!.GetValue(body)!;
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
            var list = Assert.IsAssignableFrom<List<ExcecaoDto>>(ok.Value);
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
            var list = Assert.IsAssignableFrom<List<ExcecaoDto>>(ok.Value);
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

        // --------------------------------------------------------------------
        //     >>>>>>  NOVOS TESTES (ramos de erro/validação)  <<<<<<
        // --------------------------------------------------------------------

        // --------- DefinirHorario (POST /horario)
        [Fact]
        public async Task DefinirHorario_Sucesso_204()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.DefinirHorarioAsync(1, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(12), It.IsAny<CancellationToken>()))
             .ReturnsAsync(0);

            var sut = SUT(db, hMock: h);
            var res = await sut.DefinirHorario(1, new DefinirHorarioRequest
            {
                DiaSemana = "seg",
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(12)
            }, CancellationToken.None);

            Assert.IsType<NoContentResult>(res);
            h.VerifyAll();
        }

        [Fact]
        public async Task DefinirHorario_ModelStateInvalido_400()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);
            sut.ModelState.AddModelError("DiaSemana", "obrigatório");

            var res = await sut.DefinirHorario(1, new DefinirHorarioRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task DefinirHorario_KeyNotFound_404()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.DefinirHorarioAsync(9, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(10), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("medico não existe"));

            var sut = SUT(db, hMock: h);
            var res = await sut.DefinirHorario(9, new DefinirHorarioRequest { DiaSemana = "seg", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10) }, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
            h.VerifyAll();
        }

        [Fact]
        public async Task DefinirHorario_ArgumentException_400()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.DefinirHorarioAsync(1, "Seg", TimeSpan.FromHours(12), TimeSpan.FromHours(10), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new ArgumentException("HoraInicio deve ser anterior"));

            var sut = SUT(db, hMock: h);
            var res = await sut.DefinirHorario(1, new DefinirHorarioRequest { DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(12), HoraFim = TimeSpan.FromHours(10) }, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
            h.VerifyAll();
        }

        [Fact]
        public async Task DefinirHorario_InvalidOperation_409()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.DefinirHorarioAsync(1, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(12), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("sobreposição"));

            var sut = SUT(db, hMock: h);
            var res = await sut.DefinirHorario(1, new DefinirHorarioRequest { DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(12) }, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(res);
            h.VerifyAll();
        }

        // --------- AtualizarHorario (PUT /horario/{id}) — erros
        [Fact]
        public async Task AtualizarHorario_ModelStateInvalido_400()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);
            sut.ModelState.AddModelError("HoraInicio", "inválida");

            var res = await sut.AtualizarHorario(1, 1, new AtualizarHorarioRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task AtualizarHorario_KeyNotFound_404()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.AtualizarHorarioAsync(5, 99, "Seg", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("não existe"));

            var sut = SUT(db, hMock: h);
            var res = await sut.AtualizarHorario(5, 99, new AtualizarHorarioRequest { DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9) }, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
            h.VerifyAll();
        }

        [Fact]
        public async Task AtualizarHorario_Unauthorized_403Forbid()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.AtualizarHorarioAsync(5, 2, "Seg", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new UnauthorizedAccessException("sem permissão"));

            var sut = SUT(db, hMock: h);
            var res = await sut.AtualizarHorario(5, 2, new AtualizarHorarioRequest { DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9) }, CancellationToken.None);

            Assert.IsType<ForbidResult>(res);
            h.VerifyAll();
        }

        [Fact]
        public async Task AtualizarHorario_ArgumentException_400()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.AtualizarHorarioAsync(1, 1, "Seg", TimeSpan.FromHours(10), TimeSpan.FromHours(9), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new ArgumentException("ordem horas"));

            var sut = SUT(db, hMock: h);
            var res = await sut.AtualizarHorario(1, 1, new AtualizarHorarioRequest { DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(10), HoraFim = TimeSpan.FromHours(9) }, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
            h.VerifyAll();
        }

        [Fact]
        public async Task AtualizarHorario_InvalidOperation_409()
        {
            using var db = TestDb.Create();
            var h = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
            h.Setup(x => x.AtualizarHorarioAsync(1, 1, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(12), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("overlap"));

            var sut = SUT(db, hMock: h);
            var res = await sut.AtualizarHorario(1, 1, new AtualizarHorarioRequest { DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(12) }, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(res);
            h.VerifyAll();
        }

        // --------- RegistarExcecao (POST /excecoes)
        [Fact]
        public async Task RegistarExcecao_Sucesso_204()
        {
            using var db = TestDb.Create();
            var e = new Mock<IHorarioExcecaoMedico>(MockBehavior.Strict);
            e.Setup(x => x.RegistarExcecaoAsync(3, new DateOnly(2025, 10, 27), TimeSpan.FromHours(9), TimeSpan.FromHours(10), true, "Motivo", It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

            var sut = SUT(db, eMock: e);
            var res = await sut.RegistarExcecao(3, new RegistarExcecaoRequest
            {
                Data = new DateOnly(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(10),
                IsReducao = true,
                Motivo = "Motivo"
            }, CancellationToken.None);

            Assert.IsType<NoContentResult>(res);
            e.VerifyAll();
        }

        [Fact]
        public async Task RegistarExcecao_ModelStateInvalido_400()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);
            sut.ModelState.AddModelError("Data", "obrigatória");

            var res = await sut.RegistarExcecao(1, new RegistarExcecaoRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task RegistarExcecao_KeyNotFound_404()
        {
            using var db = TestDb.Create();
            var e = new Mock<IHorarioExcecaoMedico>(MockBehavior.Strict);
            e.Setup(x => x.RegistarExcecaoAsync(3, It.IsAny<DateOnly>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), true, "Motivo", It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("medico não existe"));

            var sut = SUT(db, eMock: e);
            var res = await sut.RegistarExcecao(3, new RegistarExcecaoRequest
            {
                Data = new DateOnly(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(10),
                IsReducao = true,
                Motivo = "Motivo"
            }, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
            e.VerifyAll();
        }

        [Fact]
        public async Task RegistarExcecao_ArgumentException_400()
        {
            using var db = TestDb.Create();
            var e = new Mock<IHorarioExcecaoMedico>(MockBehavior.Strict);
            e.Setup(x => x.RegistarExcecaoAsync(1, It.IsAny<DateOnly>(), TimeSpan.FromHours(11), TimeSpan.FromHours(10), false, null, It.IsAny<CancellationToken>()))
             .ThrowsAsync(new ArgumentException("HoraInicio >= HoraFim"));

            var sut = SUT(db, eMock: e);
            var res = await sut.RegistarExcecao(1, new RegistarExcecaoRequest
            {
                Data = new DateOnly(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(11),
                HoraFim = TimeSpan.FromHours(10),
                IsReducao = false
            }, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
            e.VerifyAll();
        }

        // --------- GetHorario / GetExcecao (NotFound/Ok)
        [Fact]
        public async Task GetHorario_NotFound_404()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);

            var res = await sut.GetHorario(5, 999, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetHorario_Encontrado_200()
        {
            using var db = TestDb.Create();
            db.HorariosTrabalhoMedicos.Add(new HorarioTrabalhoMedico { Id = 10, MedicoId = 5, DiaSemana = "Seg", HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9) });
            await db.SaveChangesAsync();

            var sut = SUT(db);
            var res = await sut.GetHorario(5, 10, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetExcecao_NotFound_404()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);

            var res = await sut.GetExcecao(2, 123, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetExcecao_Encontrado_200()
        {
            using var db = TestDb.Create();
            db.HorariosExcecaoMedicos.Add(new HorarioExcecaoMedico
            {
                Id = 4, MedicoId = 2, Data = new DateTime(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10),
                IsReducao = true, Motivo = "ok"
            });
            await db.SaveChangesAsync();

            var sut = SUT(db);
            var res = await sut.GetExcecao(2, 4, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.NotNull(ok.Value);
        }

        // --------- AtualizarExcecao (PUT /excecoes/{id})
        [Fact]
        public async Task AtualizarExcecao_ModelStateInvalido_400()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);
            sut.ModelState.AddModelError("Data", "obrigatória");

            var res = await sut.AtualizarExcecao(1, 1, new AtualizarExcecaoRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task AtualizarExcecao_HoraInicioMaiorOuIgual_400()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);

            var req = new AtualizarExcecaoRequest
            {
                Data = new DateOnly(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(10),
                HoraFim = TimeSpan.FromHours(10),
                IsReducao = true,
                Motivo = "x"
            };

            var res = await sut.AtualizarExcecao(1, 1, req, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task AtualizarExcecao_NotFound_404()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);

            var req = new AtualizarExcecaoRequest
            {
                Data = new DateOnly(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(10),
                IsReducao = false
            };

            var res = await sut.AtualizarExcecao(7, 999, req, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task AtualizarExcecao_Sucesso_200()
        {
            using var db = TestDb.Create();
            var e = new HorarioExcecaoMedico
            {
                Id = 5, MedicoId = 7, Data = new DateTime(2025, 10, 27),
                HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9),
                IsReducao = false, Motivo = "old"
            };
            db.HorariosExcecaoMedicos.Add(e);
            await db.SaveChangesAsync();

            var sut = SUT(db);

            var req = new AtualizarExcecaoRequest
            {
                Data = new DateOnly(2025, 10, 28),
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(10),
                IsReducao = true,
                Motivo = "novo"
            };

            var res = await sut.AtualizarExcecao(7, 5, req, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(res);
            dynamic dto = ok.Value!;
            Assert.Equal(5, (int)dto.Id);
            Assert.True((bool)dto.IsReducao);
            Assert.Equal("novo", (string?)dto.Motivo);
        }

        // --------- RemoverHorario (DELETE /horario/{id})
        [Fact]
        public async Task RemoverHorario_NotFound_404()
        {
            using var db = TestDb.Create();
            var sut = SUT(db);

            var res = await sut.RemoverHorario(5, 321, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task RemoverHorario_Sucesso_204()
        {
            using var db = TestDb.Create();
            db.HorariosTrabalhoMedicos.Add(new HorarioTrabalhoMedico
            {
                Id = 12, MedicoId = 5, DiaSemana = "Seg",
                HoraInicio = TimeSpan.FromHours(8), HoraFim = TimeSpan.FromHours(9)
            });
            await db.SaveChangesAsync();

            var sut = SUT(db);
            var res = await sut.RemoverHorario(5, 12, CancellationToken.None);

            Assert.IsType<NoContentResult>(res);
            Assert.False(db.HorariosTrabalhoMedicos.Any(h => h.Id == 12));
        }
    }
}
