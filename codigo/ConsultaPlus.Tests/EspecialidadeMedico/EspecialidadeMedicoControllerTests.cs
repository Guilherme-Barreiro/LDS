using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Services;
using ConsultaPlus.Tests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using EspecialidadeMedicoModel = ConsultaPlus.Core.Models.EspecialidadeMedico;


    namespace ConsultaPlus.Tests.EspecialidadeMedico
{
    public class EspecialidadeMedicoControllerTests
    {
        private readonly Mock<IEspecialidadeMedicoService> _svc;
        private readonly EspecialidadeMedicoController _controller;

        public EspecialidadeMedicoControllerTests()
        {
            _svc = new Mock<IEspecialidadeMedicoService>(MockBehavior.Strict);
            _controller = new EspecialidadeMedicoController(_svc.Object);
        }

        private static string GetMessage(object? value)
            => value?.GetType().GetProperty("message")?.GetValue(value)?.ToString() ?? string.Empty;

        [Fact]
        public async Task AddEspecialidadeMedico_DeveRetornarOk_DeveRetornarCreated()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 10, EspecialidadeId = 11 };
            _svc.Setup(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId)).Returns(Task.CompletedTask);

            var result = await _controller.AddEspecialidadeMedico(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, objectResult.StatusCode);

            var message = GetMessage(objectResult.Value);
            Assert.Equal("Especialidade associada ao medico com sucesso.", message);

            _svc.Verify(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task AddEspecialidadeMedico_MedicoNaoEncontrado_DeveRetornarNotFound()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 999, EspecialidadeId = 11 };

            _svc.Setup(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId)).ThrowsAsync(new KeyNotFoundException("Médico não encontrado."));

            var result = await _controller.AddEspecialidadeMedico(dto);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var message = GetMessage(nf.Value);
            Assert.Equal("Médico não encontrado.", message);

            _svc.Verify(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task AddEspecialidadeMedico_EspecialidadeNaoEncontrada_DeveRetornarNotFound()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 10, EspecialidadeId = 999 };

            _svc.Setup(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId)).ThrowsAsync(new KeyNotFoundException("Especialidade não encontrada."));

            var result = await _controller.AddEspecialidadeMedico(dto);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var message = GetMessage(nf.Value);
            Assert.Equal("Especialidade não encontrada.", message);

            _svc.Verify(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task AddEspecialidadeMedico_AssociacaoJaExiste_DeveRetornarConflict()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 10, EspecialidadeId = 11 };
            _svc.Setup(s => s.AddAsync(dto.MedicoId, dto.EspecialidadeId))
                .ThrowsAsync(new InvalidOperationException("Associação já existe."));

            var result = await _controller.AddEspecialidadeMedico(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflict.Value);
            Assert.Equal("Associação já existe.", message);
        }

        [Fact]
        public async Task DeleteEspecialidadeMedico_Sucesso_DeveRetornarNoContent()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 10, EspecialidadeId = 11 };

            _svc.Setup(s => s.DeleteAsync(dto.MedicoId, dto.EspecialidadeId)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteEspecialidadeMedico(dto);

            Assert.IsType<NoContentResult>(result);

            _svc.Verify(s => s.DeleteAsync(dto.MedicoId, dto.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task DeleteEspecialidadeMedico_AssociacaoNaoEncontrada_DeveRetornarNotFound()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 10, EspecialidadeId = 11 };

            _svc.Setup(s => s.DeleteAsync(dto.MedicoId, dto.EspecialidadeId)).ThrowsAsync(new KeyNotFoundException("Associação não encontrada."));

            var result = await _controller.DeleteEspecialidadeMedico(dto);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var message = GetMessage(nf.Value);
            Assert.Equal("Associação não encontrada.", message);

            _svc.Verify(s => s.DeleteAsync(dto.MedicoId, dto.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task DeleteEspecialidadeMedico_ErroBaseDeDados_DeveRetornarConflict()
        {
            var dto = new EspecialidadeMedicoDTO { MedicoId = 10, EspecialidadeId = 11 };

            _svc.Setup(s => s.DeleteAsync(dto.MedicoId, dto.EspecialidadeId)).ThrowsAsync(new DbUpdateException());

            var result = await _controller.DeleteEspecialidadeMedico(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflict.Value);
            Assert.Equal("Nao foi possivel remover a especialidade devido a um conflito na base de dados.", message);

            _svc.Verify(s => s.DeleteAsync(dto.MedicoId, dto.EspecialidadeId), Times.Once);
        }

        [Fact]
        public async Task GetMedicosByEspecialidadeId_SemMedicos_DeveRetornarNotFound()
        {
            _svc.Setup(s => s.GetMedicosByEspecialidadeIdAsync(999))
                .ReturnsAsync(Enumerable.Empty<Medico>());

            var result = await _controller.GetMedicosByEspecialidadeId(999);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var message = GetMessage(nf.Value);
            Assert.Equal("Nenhum medico encontrado para essa especialidade.", message);

            _svc.Verify(s => s.GetMedicosByEspecialidadeIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task GetMedicosByEspecialidadeId_DeveRetornarOk_ComMedicos()
        {
            _svc.Setup(s => s.GetMedicosByEspecialidadeIdAsync(1))
                .ReturnsAsync(new List<Medico> { new Medico { Id = 1, NomeCompleto = "Dr. João" } });

            var result = await _controller.GetMedicosByEspecialidadeId(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var medicos = Assert.IsAssignableFrom<IEnumerable<Medico>>(ok.Value);
            Assert.Single(medicos);

            _svc.Verify(s => s.GetMedicosByEspecialidadeIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetEspecialidadesByMedicoId_SemMedicos_DeveRetornarNotFound()
        {
            _svc.Setup(s => s.GetEspecialidadesByMedicoIdAsync(999))
                .ReturnsAsync(Enumerable.Empty<Especialidade>());

            var result = await _controller.GetEspecialidadesByMedicoId(999);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var message = GetMessage(nf.Value);
            Assert.Equal("Nenhuma especialidade encontrada para esse medico.", message);

            _svc.Verify(s => s.GetEspecialidadesByMedicoIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task GetEspecialidadesByMedicoId_DeveRetornarOk_ComEspecialidades()
        {
            _svc.Setup(s => s.GetEspecialidadesByMedicoIdAsync(1))
                .ReturnsAsync(new List<Especialidade> { new Especialidade { Id = 1, Nome = "Cardiologia" } });

            var result = await _controller.GetEspecialidadesByMedicoId(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var especialidades = Assert.IsAssignableFrom<IEnumerable<Especialidade>>(ok.Value);
            Assert.Single(especialidades);
            _svc.Verify(s => s.GetEspecialidadesByMedicoIdAsync(1), Times.Once);
        }
    }
}
