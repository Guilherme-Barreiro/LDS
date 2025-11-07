using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models.Relatorios;
using ConsultaPlus.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Tests.Relatorios
{
    public class RelatorioServiceTests
    {
        private readonly Mock<IRelatorioRepository> _repoMock;
        private readonly RelatorioService _service;

        public RelatorioServiceTests()
        {
            _repoMock = new Mock<IRelatorioRepository>(MockBehavior.Strict);
            _service = new RelatorioService(_repoMock.Object);
        }

        [Fact]
        public async Task GetConsultasPorPeriodoAsync_ParametrosValidos_RetornaLista()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = 1;

            var expectedResult = new List<ConsultasPorPeriodo>
            {
                new ConsultasPorPeriodo
                {
                    MedicoNome = "Dr. Teste",
                    EspecialidadeNome = "Cardiologia",
                    TotalConsultas = 10,
                    ConsultasRealizadas = 8,
                    ConsultasNaoCompareceram = 2,
                    ConsultasCanceladas = 0
                }
            };

            _repoMock.Setup(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Dr. Teste", result[0].MedicoNome);
            _repoMock.Verify(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodoAsync_SemMedicoId_RetornaLista()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = null;

            var expectedResult = new List<ConsultasPorPeriodo>
            {
                new ConsultasPorPeriodo
                {
                    MedicoNome = "Dr. Teste 1",
                    EspecialidadeNome = "Cardiologia",
                    TotalConsultas = 10,
                    ConsultasRealizadas = 8,
                    ConsultasNaoCompareceram = 2,
                    ConsultasCanceladas = 0
                },
                new ConsultasPorPeriodo
                {
                    MedicoNome = "Dr. Teste 2",
                    EspecialidadeNome = "Pediatria",
                    TotalConsultas = 15,
                    ConsultasRealizadas = 12,
                    ConsultasNaoCompareceram = 3,
                    ConsultasCanceladas = 0
                }
            };

            _repoMock.Setup(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _repoMock.Verify(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodoAsync_DataInicioDepoisDataFim_DeveRetornarArgumentException()
        {
            var dataInicio = new DateTime(2024, 12, 31);
            var dataFim = new DateTime(2024, 1, 1);
            int? medicoId = null;

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId));

            Assert.Equal("A data de início não pode ser posterior à data de fim.", exception.Message);
            _repoMock.Verify(r => r.GetConsultasPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetConsultasPorPeriodoAsync_ComMesmasDatas_RetornaLista()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 1, 1);
            int? medicoId = null;

            var expectedResult = new List<ConsultasPorPeriodo>
            {
                new ConsultasPorPeriodo
                {
                    MedicoNome = "Dr. Teste",
                    EspecialidadeNome = "Cardiologia",
                    TotalConsultas = 5,
                    ConsultasRealizadas = 4,
                    ConsultasNaoCompareceram = 1,
                    ConsultasCanceladas = 0
                }
            };

            _repoMock.Setup(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId);

            Assert.NotNull(result);
            Assert.Single(result);
            _repoMock.Verify(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_ParametrosValidos_RetornaLista()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = 1;
            int? especialidadeId = 2;

            var expectedResult = new TaxaNaoComparecimento
            {
                TaxaGlobal = 10.5m,
                TotalConsultas = 100,
                TotalNaoCompareceram = 10,
                PorMedico = new List<TaxaNaoComparecimentoPorMedico>
                {
                    new TaxaNaoComparecimentoPorMedico
                    {
                        MedicoNome = "Dr. Teste",
                        EspecialidadeNome = "Cardiologia",
                        Taxa = 10.5m,
                        TotalConsultas = 100,
                        NaoCompareceram = 10
                    }
                }
            };

            _repoMock.Setup(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId);

            Assert.NotNull(result);
            Assert.Equal(10.5m, result.TaxaGlobal);
            Assert.Single(result.PorMedico);
            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_SemDados_RetornaLista()
        {
            DateTime? dataInicio = null;
            DateTime? dataFim = null;
            int? medicoId = null;
            int? especialidadeId = null;

            var expectedResult = new TaxaNaoComparecimento
            {
                TaxaGlobal = 5.0m,
                TotalConsultas = 200,
                TotalNaoCompareceram = 10,
                PorMedico = new List<TaxaNaoComparecimentoPorMedico>()
            };

            _repoMock.Setup(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId);

            Assert.NotNull(result);
            Assert.Equal(5.0m, result.TaxaGlobal);
            Assert.Empty(result.PorMedico);
            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_PeriodoExcedeUmAno_DeveRetornarArgumentException()
        {
            var dataInicio = new DateTime(2023, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = null;
            int? especialidadeId = null;

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId));

            Assert.Equal("O período não pode exceder 1 ano.", exception.Message);
            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_ExatamenteUmAno_RetornaLista()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = null;
            int? especialidadeId = null;

            var expectedResult = new TaxaNaoComparecimento
            {
                TaxaGlobal = 8.0m,
                TotalConsultas = 150,
                TotalNaoCompareceram = 12,
                PorMedico = new List<TaxaNaoComparecimentoPorMedico>()
            };

            _repoMock.Setup(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId);

            Assert.NotNull(result);
            Assert.Equal(8.0m, result.TaxaGlobal);
            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_SoComDataInicio_RetornaLista()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            DateTime? dataFim = null;
            int? medicoId = null;
            int? especialidadeId = null;

            var expectedResult = new TaxaNaoComparecimento
            {
                TaxaGlobal = 7.5m,
                TotalConsultas = 80,
                TotalNaoCompareceram = 6,
                PorMedico = new List<TaxaNaoComparecimentoPorMedico>()
            };

            _repoMock.Setup(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId);

            Assert.NotNull(result);
            Assert.Equal(7.5m, result.TaxaGlobal);
            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_SoComDataFim_RetornaLista()
        {
            DateTime? dataInicio = null;
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = null;
            int? especialidadeId = null;

            var expectedResult = new TaxaNaoComparecimento
            {
                TaxaGlobal = 6.0m,
                TotalConsultas = 120,
                TotalNaoCompareceram = 7,
                PorMedico = new List<TaxaNaoComparecimentoPorMedico>()
            };

            _repoMock.Setup(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId))
                .ReturnsAsync(expectedResult);

            var result = await _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId);

            Assert.NotNull(result);
            Assert.Equal(6.0m, result.TaxaGlobal);
            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodoAsync_Invalido_DeveRetornaInvalidOperation()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = null;

            _repoMock.Setup(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId));

            _repoMock.Verify(r => r.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimentoAsync_Invalido_DeveRetornaInvalidOperation()
        {
            var dataInicio = new DateTime(2024, 1, 1);
            var dataFim = new DateTime(2024, 12, 31);
            int? medicoId = null;
            int? especialidadeId = null;

            _repoMock.Setup(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId));

            _repoMock.Verify(r => r.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId), Times.Once);
        }
    }
}
