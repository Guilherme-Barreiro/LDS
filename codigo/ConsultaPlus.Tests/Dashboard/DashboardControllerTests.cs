using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Consultas;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ConsultaPlus.Tests.Unit.Controllers
{
    public class DashboardControllerTests
    {
        private readonly Mock<IConsultaRepository> _repo = new();

        private static Consulta C(
            int id,
            int medicoId,
            int pacienteId,
            DateTime inicio,
            int duracaoMin,
            string estado = "Confirmada",
            int especialidadeId = 1,
            int? salaId = 1)
            => new Consulta
            {
                Id = id,
                MedicoId = medicoId,
                PacienteId = pacienteId,
                DataConsulta = inicio,
                Duracao = duracaoMin,
                Estado = estado,
                EspecialidadeId = especialidadeId,
                SalaId = salaId ?? 0 // se for realmente nullable no teu modelo, ajusta
            };

        private DashboardController CreateController() => new(_repo.Object);

        // ========== MÉDICO /AGENDA ==========

        [Fact]
        public async Task GetAgendaMedico_ReturnsOk_AndMapsDtos()
        {
            // Arrange
            var medicoId = 7;
            var from = new DateTime(2025, 11, 04);
            var to = new DateTime(2025, 11, 04); // controller irá transformar para endExclusive = 2025-11-05
            var endExclusiveEsperado = new DateTime(2025, 11, 05);

            var consultas = new List<Consulta>
            {
                C(1, medicoId, 12, new DateTime(2025,11,04,10,0,0), 30, "Confirmada", especialidadeId: 3, salaId: 2),
                C(2, medicoId, 13, new DateTime(2025,11,04,14,0,0), 60, "Pendente",    especialidadeId: 3, salaId: 2)
            };

            _repo.Setup(r => r.GetByMedicoRangeAsync(
                    medicoId,
                    It.Is<DateTime>(d => d == from),
                    It.Is<DateTime>(d => d == endExclusiveEsperado), // exclusivo
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(consultas);

            var sut = CreateController();

            // Act
            var result = await sut.GetAgendaMedico(medicoId, from, to, onlyConfirmed: false, ct: default);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<IEnumerable<AgendaItemDto>>(ok.Value);
            var list = items.ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].ConsultaId);
            Assert.Equal(new DateTime(2025, 11, 04, 10, 0, 0), list[0].Inicio);
            Assert.Equal(new DateTime(2025, 11, 04, 10, 30, 0), list[0].Fim);
            Assert.Equal("Confirmada", list[0].Estado);
            Assert.Equal(12, list[0].PacienteId);
            Assert.Equal(2, list[0].SalaId);

            _repo.VerifyAll();
        }

        [Fact]
        public async Task GetAgendaMedico_BadRequest_When_ToEarlierThanFrom()
        {
            // Arrange
            var sut = CreateController();
            var medicoId = 7;
            var from = new DateTime(2025, 11, 05);
            var to = new DateTime(2025, 11, 04); // to < from

            // Act
            var result = await sut.GetAgendaMedico(medicoId, from, to, onlyConfirmed: false, ct: default);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var msg = Assert.IsType<string>(bad.Value);
            Assert.Contains("'to' deve ser >=", msg, StringComparison.OrdinalIgnoreCase);

            _repo.Verify(r => r.GetByMedicoRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAgendaMedico_Defaults_WhenNullParams()
        {
            // Arrange
            var medicoId = 10;

            // vamos capturar os valores que o controller envia ao repo
            DateTime capturedFrom = default, capturedToExclusive = default;

            _repo.Setup(r => r.GetByMedicoRangeAsync(
                    medicoId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    false,
                    It.IsAny<CancellationToken>()))
                 .Callback<int, DateTime, DateTime, bool, CancellationToken>((_, f, t, _, __) =>
                 {
                     capturedFrom = f;
                     capturedToExclusive = t;
                 })
                 .ReturnsAsync(new List<Consulta>());

            var sut = CreateController();

            // Act
            var result = await sut.GetAgendaMedico(medicoId, from: null, to: null, onlyConfirmed: false, ct: default);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            // Os defaults: start = UtcToday, endExclusive = start.AddDays(28).AddDays(1)
            var todayUtc = DateTime.UtcNow.Date;
            Assert.Equal(todayUtc, capturedFrom);
            Assert.Equal(todayUtc.AddDays(29), capturedToExclusive); // 28 + 1 (exclusivo)
        }

        // ========== PACIENTE / HISTÓRICO ==========

        [Fact]
        public async Task GetHistoricoPaciente_ReturnsPaged_AndMapsDtos()
        {
            // Arrange
            var pacienteId = 12;
            var page = 1;
            var pageSize = 2;

            var consultas = new List<Consulta>
            {
                C(1, medicoId: 7, pacienteId, new DateTime(2025,11,04,10,0,0), 30, "Confirmada", especialidadeId: 5, salaId: 3),
                C(2, medicoId: 7, pacienteId, new DateTime(2025,11,01,15,0,0), 60, "Pendente",    especialidadeId: 5, salaId: 3)
            };

            var paged = new PagedResult<Consulta>(Total: 10, Page: page, PageSize: pageSize, Items: consultas);

            _repo.Setup(r => r.GetByPacienteAsync(
                    pacienteId,
                    null,
                    null,
                    page,
                    pageSize,
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(paged);

            var sut = CreateController();

            // Act
            var result = await sut.GetHistoricoPaciente(pacienteId, page, pageSize, from: null, to: null, ct: default);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<PagedListDto<ConsultaPacienteDto>>(ok.Value);

            Assert.Equal(10, dto.Total);
            Assert.Equal(page, dto.Page);
            Assert.Equal(pageSize, dto.PageSize);
            Assert.Equal(2, dto.Items.Count);

            var first = dto.Items[0];
            Assert.Equal(1, first.Id);
            Assert.Equal(new DateTime(2025, 11, 04, 10, 0, 0), first.Inicio);
            Assert.Equal(new DateTime(2025, 11, 04, 10, 30, 0), first.Fim);
            Assert.Equal("Confirmada", first.Estado);
            Assert.Equal(7, first.MedicoId);
            Assert.Equal(5, first.EspecialidadeId);
            Assert.Equal(3, first.SalaId);

            _repo.VerifyAll();
        }

        [Fact]
        public async Task GetHistoricoPaciente_PassesFiltersAndPaging()
        {
            // Arrange
            var pacienteId = 99;
            var from = new DateTime(2025, 11, 01);
            var to = new DateTime(2025, 11, 30);
            var page = 2;
            var pageSize = 25;

            var paged = new PagedResult<Consulta>(Total: 0, Page: page, PageSize: pageSize, Items: new List<Consulta>());

            _repo.Setup(r => r.GetByPacienteAsync(
                    pacienteId,
                    from,
                    to,
                    page,
                    pageSize,
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(paged);

            var sut = CreateController();

            // Act
            var result = await sut.GetHistoricoPaciente(pacienteId, page, pageSize, from, to, default);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<PagedListDto<ConsultaPacienteDto>>(ok.Value);
            Assert.Equal(0, dto.Total);
            Assert.Empty(dto.Items);

            _repo.VerifyAll();
        }
    }
}
