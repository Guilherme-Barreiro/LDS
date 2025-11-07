using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Consultas;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Tests.Consultas
{
    public class ConsultasControllerTests
    {
        private readonly Mock<IConsultaService> _svc;
        private readonly ConsultasController _controller;

        public ConsultasControllerTests()
        {
            _svc = new Mock<IConsultaService>(MockBehavior.Strict);
            _controller = new ConsultasController(_svc.Object);
        }

        [Fact]
        public async Task GetAll_DeveRetornarOk_ComListaMapeada()
        {
            var dt1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var dt2 = new DateTime(2025, 1, 2, 11, 0, 0, DateTimeKind.Utc);

            var data = new List<Consulta>
            {
                new Consulta { Id = 1, PacienteId = 10, MedicoId = 100, SalaId = 1000, EspecialidadeId = 200, DataConsulta = dt1, Duracao = 30, Estado = "Marcada" },
                new Consulta { Id = 2, PacienteId = 11, MedicoId = 101, SalaId = 1001, EspecialidadeId = 201, DataConsulta = dt2, Duracao = 45, Estado = "Concluida" },
            };
            _svc.Setup(r => r.GetAllAsync(default)).ReturnsAsync(data.AsEnumerable());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<ConsultaResponseDto>>(ok.Value);
            Assert.Equal(2, list.Count());

            Assert.Contains(list, c =>
                c.Id == 1 && c.PacienteId == 10 && c.MedicoId == 100 &&
                c.SalaId == 1000 && c.EspecialidadeId == 200 &&
                c.DataConsulta == dt1 &&
                c.Duracao == 30 && c.Estado == "Marcada");

            Assert.Contains(list, c =>
                c.Id == 2 && c.PacienteId == 11 && c.MedicoId == 101 &&
                c.SalaId == 1001 && c.EspecialidadeId == 201 &&
                c.DataConsulta == dt2 &&
                c.Duracao == 45 && c.Estado == "Concluida");

            _svc.Verify(r => r.GetAllAsync(default), Times.Once);
        }

        [Fact]
        public async Task GetById_Existente_DeveRetornarOk_ComDto()
        {
            var dt = new DateTime(2025, 1, 3, 9, 0, 0, DateTimeKind.Utc);
            var c = new Consulta
            {
                Id = 7,
                PacienteId = 10,
                MedicoId = 100,
                SalaId = 1000,
                EspecialidadeId = 200,
                DataConsulta = dt,
                Duracao = 20,
                Estado = "Marcada"
            };
            _svc.Setup(r => r.GetByIdAsync(7, default)).ReturnsAsync(c);

            var result = await _controller.GetById(7);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ConsultaResponseDto>(ok.Value);
            Assert.Equal(7, dto.Id);
            Assert.Equal(10, dto.PacienteId);
            Assert.Equal(100, dto.MedicoId);
            Assert.Equal(1000, dto.SalaId);
            Assert.Equal(200, dto.EspecialidadeId);
            Assert.Equal(dt, dto.DataConsulta);
            Assert.Equal(20, dto.Duracao);
            Assert.Equal("Marcada", dto.Estado);

            _svc.Verify(r => r.GetByIdAsync(7, default), Times.Once);
        }

        [Fact]
        public async Task GetById_Inexistente_DeveRetornarNotFound_ComMensagem()
        {
            _svc.Setup(r => r.GetByIdAsync(123, default)).ReturnsAsync((Consulta?)null);

            var result = await _controller.GetById(123);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var messageProp = nf.Value!.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Contains("Consulta 123 não encontrada", messageProp!.GetValue(nf.Value)?.ToString());

            _svc.Verify(r => r.GetByIdAsync(123, default), Times.Once);
        }

        [Fact]
        public async Task GetByMedico_DeveFiltrarPorMedicoId()
        {
            var now = DateTime.UtcNow;
            var data = new List<Consulta>
            {
                new Consulta { Id = 1, MedicoId = 5, PacienteId = 10, SalaId = 1, EspecialidadeId = 1, DataConsulta = now, Duracao = 30, Estado = "Marcada" },
                new Consulta { Id = 2, MedicoId = 5, PacienteId = 11, SalaId = 2, EspecialidadeId = 1, DataConsulta = now, Duracao = 30, Estado = "Marcada" },
                new Consulta { Id = 3, MedicoId = 9, PacienteId = 12, SalaId = 3, EspecialidadeId = 2, DataConsulta = now, Duracao = 30, Estado = "Marcada" },
            };

            var consultasMedico5 = data.Where(c => c.MedicoId == 5).ToList();

            _svc.Setup(r => r.GetByMedicoAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(consultasMedico5);

            var result = await _controller.GetByMedico(5);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseList = Assert.IsAssignableFrom<IEnumerable<ConsultaResponseDto>>(okResult.Value);

            Assert.Equal(2, responseList.Count());
            Assert.All(responseList, consultaDto => Assert.Equal(5, consultaDto.MedicoId));


            _svc.Verify(r => r.GetByMedicoAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByPaciente_DeveFiltrarPorPacienteId()
        {
            var now = DateTime.UtcNow;
            var data = new List<Consulta>
            {
                new Consulta { Id = 1, MedicoId = 5, PacienteId = 10, SalaId = 1, EspecialidadeId = 1, DataConsulta = now, Duracao = 30, Estado = "Marcada" },
                new Consulta { Id = 2, MedicoId = 6, PacienteId = 10, SalaId = 2, EspecialidadeId = 1, DataConsulta = now, Duracao = 30, Estado = "Marcada" },
                new Consulta { Id = 3, MedicoId = 9, PacienteId = 99, SalaId = 3, EspecialidadeId = 2, DataConsulta = now, Duracao = 30, Estado = "Marcada" },
            };

            var consultasPaciente10 = data.Where(c => c.PacienteId == 10).ToList();

            _svc.Setup(r => r.GetByPacienteAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(consultasPaciente10);

            var result = await _controller.GetByPaciente(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<ConsultaResponseDto>>(ok.Value);
            Assert.Equal(2, list.Count());
            Assert.All(list, c => Assert.Equal(10, c.PacienteId));

            _svc.Verify(r => r.GetByPacienteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_DeveAdicionar_ERetornar201_ComDtoMapeado()
        {
            var dt = new DateTime(2025, 1, 5, 14, 0, 0, DateTimeKind.Utc);

            var dto = new CreateConsultaDto
            {
                PacienteId = 10,
                MedicoId = 100,
                SalaId = 1000,
                EspecialidadeId = 200,
                DataConsulta = dt,
            };

            var consultaCriada = new Consulta
            {
                Id = 55,
                PacienteId = 10,
                MedicoId = 100,
                SalaId = 1000,
                EspecialidadeId = 200,
                DataConsulta = dt,
                Duracao = 30,
                Estado = "Confirmada"
            };

            _svc.Setup(r => r.CreateAsync(It.IsAny<Consulta>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(consultaCriada);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);

            var body = Assert.IsType<ConsultaResponseDto>(created.Value);
            Assert.Equal(55, body.Id);
            Assert.Equal(10, body.PacienteId);
            Assert.Equal(100, body.MedicoId);
            Assert.Equal(1000, body.SalaId);
            Assert.Equal(200, body.EspecialidadeId);
            Assert.Equal(dt, body.DataConsulta);
            Assert.Equal(30, body.Duracao);
            Assert.Equal("Confirmada", body.Estado);

            _svc.Verify(r => r.CreateAsync(It.Is<Consulta>(c =>
                c.PacienteId == 10 &&
                c.MedicoId == 100 &&
                c.SalaId == 1000 &&
                c.EspecialidadeId == 200 &&
                c.DataConsulta == dt 
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_DeveInvocarRepositorio_ERetornarNoContent()
        {
            _svc.Setup(r => r.DeleteAsync(9, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _controller.Delete(9);

            Assert.IsType<NoContentResult>(result);
            _svc.Verify(r => r.DeleteAsync(9, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
