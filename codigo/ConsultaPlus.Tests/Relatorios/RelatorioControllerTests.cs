using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models.Relatorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ConsultaPlus.Tests.Relatorios
{
    public class RelatorioControllerTests
    {
        private readonly Mock<IRelatorioService> _svc;
        private readonly RelatorioController _controller;

        public RelatorioControllerTests()
        {
            _svc = new Mock<IRelatorioService>(MockBehavior.Strict);
            _controller = new RelatorioController(_svc.Object);
        }

        private static string GetMessage(object? value)
            => value?.GetType().GetProperty("message")?.GetValue(value)?.ToString() ?? string.Empty;

        [Fact]
        public async Task GetConsultasPorPeriodo_RequisicaoValida_DeveRetornarOk()
        {
            var requestDTO = new ConsultasPorPeriodoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1
            };

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

            _svc.Setup(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId))
                .ReturnsAsync(expectedResult);

            var result = await _controller.GetConsultasPorPeriodo(requestDTO);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<ConsultasPorPeriodo>>(okResult.Value);
            Assert.Single(returnValue);
            Assert.Equal("Dr. Teste", returnValue[0].MedicoNome);

            _svc.Verify(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodo_ArgumentException_DeveRetornarBadRequest()
        {
            var requestDTO = new ConsultasPorPeriodoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1
            };

            _svc.Setup(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId))
                .ThrowsAsync(new ArgumentException("Período inválido."));

            var result = await _controller.GetConsultasPorPeriodo(requestDTO);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(badRequestResult.Value);
            Assert.Equal("Período inválido.", message);

            _svc.Verify(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodo_DbUpdateException_DeveRetornarConflict()
        {
            var requestDTO = new ConsultasPorPeriodoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1
            };

            _svc.Setup(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId))
                .ThrowsAsync(new DbUpdateException());

            var result = await _controller.GetConsultasPorPeriodo(requestDTO);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflictResult.Value);
            Assert.Equal("Nao foi possivel atualizar a especialidade devido a um conflito na base de dados.", message);

            _svc.Verify(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodo_ExcecaoGenerica_DeveRetornarInternalServerError()
        {
            var requestDTO = new ConsultasPorPeriodoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1
            };

            _svc.Setup(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId))
                .ThrowsAsync(new Exception());

            var result = await _controller.GetConsultasPorPeriodo(requestDTO);

            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var message = GetMessage(statusCodeResult.Value);
            Assert.Equal("Erro ao gerar relatório.", message);

            _svc.Verify(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimento_RequisicaoValida_DeveRetornarOk()
        {
            var requestDTO = new TaxaNaoComparecimentoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1,
                EspecialidadeId = 1
            };

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

            _svc.Setup(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId))
                .ReturnsAsync(expectedResult);

            var result = await _controller.GetTaxaNaoComparecimento(requestDTO);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<TaxaNaoComparecimento>(okResult.Value);
            Assert.Equal(10.5m, returnValue.TaxaGlobal);
            Assert.Single(returnValue.PorMedico);

            _svc.Verify(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimento_ArgumentException_DeveRetornarBadRequest()
        {
            var requestDTO = new TaxaNaoComparecimentoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1,
                EspecialidadeId = 1
            };

            _svc.Setup(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId))
                .ThrowsAsync(new ArgumentException("Período inválido."));

            var result = await _controller.GetTaxaNaoComparecimento(requestDTO);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(badRequestResult.Value);
            Assert.Equal("Período inválido.", message);

            _svc.Verify(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimento_DbUpdateException_DeveRetornarConflict()
        {
            var requestDTO = new TaxaNaoComparecimentoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1,
                EspecialidadeId = 1
            };

            _svc.Setup(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId))
                .ThrowsAsync(new DbUpdateException());

            var result = await _controller.GetTaxaNaoComparecimento(requestDTO);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflictResult.Value);
            Assert.Equal("Nao foi possivel atualizar a especialidade devido a um conflito na base de dados.", message);

            _svc.Verify(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimento_ExcecaoGenerica_DeveRetornarInternalServerError()
        {
            var requestDTO = new TaxaNaoComparecimentoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31),
                MedicoId = 1,
                EspecialidadeId = 1
            };

            _svc.Setup(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId))
                .ThrowsAsync(new Exception());

            var result = await _controller.GetTaxaNaoComparecimento(requestDTO);

            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var message = GetMessage(statusCodeResult.Value);
            Assert.Equal("Erro ao gerar relatorio de nao comparecimento.", message);

            _svc.Verify(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodo_SemMedicoId_DeveRetornarOk()
        {
            var requestDTO = new ConsultasPorPeriodoRequestDTO
            {
                DataInicio = new DateTime(2024, 1, 1),
                DataFim = new DateTime(2024, 12, 31)
               
            };

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

            _svc.Setup(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, null))
                .ReturnsAsync(expectedResult);

            var result = await _controller.GetConsultasPorPeriodo(requestDTO);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<ConsultasPorPeriodo>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);

            _svc.Verify(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, null), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimento_SemParametrosOpcionais_DeveRetornarOk()
        {
            var requestDTO = new TaxaNaoComparecimentoRequestDTO
            {
            };

            var expectedResult = new TaxaNaoComparecimento
            {
                TaxaGlobal = 5.0m,
                TotalConsultas = 200,
                TotalNaoCompareceram = 10,
                PorMedico = new List<TaxaNaoComparecimentoPorMedico>()
            };

            _svc.Setup(s => s.GetTaxaNaoComparecimentoAsync(null, null, null, null))
                .ReturnsAsync(expectedResult);

            var result = await _controller.GetTaxaNaoComparecimento(requestDTO);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<TaxaNaoComparecimento>(okResult.Value);
            Assert.Equal(5.0m, returnValue.TaxaGlobal);
            Assert.Empty(returnValue.PorMedico);

            _svc.Verify(s => s.GetTaxaNaoComparecimentoAsync(null, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetConsultasPorPeriodo_PeriodoExcedeUmAno_DeveRetornarBadRequest()
        {
            var requestDTO = new ConsultasPorPeriodoRequestDTO
            {
                DataInicio = new DateTime(2023, 1, 1),
                DataFim = new DateTime(2024, 12, 31)
            };

            _svc.Setup(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, null))
                .ThrowsAsync(new ArgumentException("O período não pode exceder 1 ano."));

            var result = await _controller.GetConsultasPorPeriodo(requestDTO);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(badRequestResult.Value);
            Assert.Equal("O período não pode exceder 1 ano.", message);

            _svc.Verify(s => s.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, null), Times.Once);
        }

        [Fact]
        public async Task GetTaxaNaoComparecimento_PeriodoExcedeUmAno_DeveRetornarBadRequest()
        {
            var requestDTO = new TaxaNaoComparecimentoRequestDTO
            {
                DataInicio = new DateTime(2023, 1, 1),
                DataFim = new DateTime(2024, 12, 31)
            };

            _svc.Setup(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, null, null))
                .ThrowsAsync(new ArgumentException("O período não pode exceder 1 ano."));

            var result = await _controller.GetTaxaNaoComparecimento(requestDTO);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(badRequestResult.Value);
            Assert.Equal("O período não pode exceder 1 ano.", message);

            _svc.Verify(s => s.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, null, null), Times.Once);
        }
    }
}
