using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.API.Controllers;                   
using ConsultaPlus.API.DTOs;                           
using ConsultaPlus.Infrastructure.Services;           
using EspecialidadeModel = ConsultaPlus.Core.Models.Especialidade;

namespace ConsultaPlus.Tests.Especialidades
{
    public class EspecialidadeControllerTests
    {
        private readonly Mock<IEspecialidadesService> _svc;
        private readonly EspecialidadeController _controller;

        public EspecialidadeControllerTests()
        {
            _svc = new Mock<IEspecialidadesService>(MockBehavior.Strict);
            _controller = new EspecialidadeController(_svc.Object);
        }

        [Fact]
        public async Task GetAll_DeveRetornarOk_ComListaMapeada()
        {
            _svc.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new[]
                {
                    new EspecialidadeModel { Id = 1, Nome = "Cardiologia" },
                    new EspecialidadeModel { Id = 2, Nome = "Dermatologia" }
                }.AsEnumerable());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var lista = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(2, lista.Count());

            _svc.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_Existente_DeveRetornarOk_ComDTO()
        {
            _svc.Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(new EspecialidadeModel { Id = 10, Nome = "Ortopedia" });

            var result = await _controller.GetById(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<EspecialidadeDTO>(ok.Value);
            Assert.Equal(10, dto.Id);
            Assert.Equal("Ortopedia", dto.Nome);

            _svc.Verify(s => s.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetById_Inexistente_DeveRetornarNotFound_ComMensagem()
        {
            _svc.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((EspecialidadeModel?)null);

            var result = await _controller.GetById(999);

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("não encontrada", nf.Value!.ToString());

            _svc.Verify(s => s.GetByIdAsync(999), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByNome_NomeInvalido_DeveRetornarBadRequest(string? nome)
        {
            var result = await _controller.GetByNome(nome!);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("obrigatório", bad.Value!.ToString());

            _svc.Verify(s => s.SearchAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetByNome_SemResultados_DeveRetornarNotFound()
        {
            _svc.Setup(s => s.SearchAsync("derma"))
                .ReturnsAsync(Enumerable.Empty<EspecialidadeModel>());

            var result = await _controller.GetByNome("derma");

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Nenhuma especialidade", nf.Value!.ToString());

            _svc.Verify(s => s.SearchAsync("derma"), Times.Once);
        }

        [Fact]
        public async Task GetByNome_ComResultados_DeveRetornarOk_ComListaDTO()
        {
            _svc.Setup(s => s.SearchAsync("derma"))
                .ReturnsAsync(new[]
                {
                    new EspecialidadeModel { Id = 2, Nome = "Dermatologia" }
                }.AsEnumerable());

            var result = await _controller.GetByNome("derma");

            var ok = Assert.IsType<OkObjectResult>(result);
            var lista = Assert.IsAssignableFrom<IEnumerable<EspecialidadeDTO>>(ok.Value);
            var item = Assert.Single(lista);
            Assert.Equal(2, item.Id);
            Assert.Equal("Dermatologia", item.Nome);

            _svc.Verify(s => s.SearchAsync("derma"), Times.Once);
        }

        [Fact]
        public async Task RegistarEspecialidade_Sucesso_DeveRetornarCreatedAtAction()
        {
            var dto = new EspecialidadeDTO { Nome = "  Neurologia  " };
            _svc.Setup(s => s.CreateAsync("  Neurologia  ")).ReturnsAsync(42);

            var result = await _controller.RegistarEspecialidade(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(EspecialidadeController.GetById), created.ActionName);

            Assert.NotNull(created.RouteValues);
            Assert.Equal(42, created.RouteValues!["id"]);

            var body = created.Value!;
            var idProp = body.GetType().GetProperty("id")!;
            var nomeProp = body.GetType().GetProperty("nome")!;

            Assert.Equal(42, (int)idProp.GetValue(body)!);
            Assert.Equal("Neurologia", (string)nomeProp.GetValue(body)!);

            _svc.Verify(s => s.CreateAsync("  Neurologia  "), Times.Once);
        }

        [Fact]
        public async Task RegistarEspecialidade_NomeInvalido_DeveRetornarBadRequest()
        {
            var dto = new EspecialidadeDTO { Nome = "   " };
            _svc.Setup(s => s.CreateAsync("   "))
                .ThrowsAsync(new ArgumentException("Nome é obrigatório."));

            var result = await _controller.RegistarEspecialidade(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nome é obrigatório.", bad.Value);

            _svc.Verify(s => s.CreateAsync("   "), Times.Once);
        }

        [Fact]
        public async Task RegistarEspecialidade_Duplicado_DeveRetornarConflict()
        {
            var dto = new EspecialidadeDTO { Nome = "Cardiologia" };
            _svc.Setup(s => s.CreateAsync("Cardiologia"))
                .ThrowsAsync(new InvalidOperationException("Já existe."));

            var result = await _controller.RegistarEspecialidade(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Já existe.", conflict.Value);

            _svc.Verify(s => s.CreateAsync("Cardiologia"), Times.Once);
        }

        [Fact]
        public async Task Update_Sucesso_DeveRetornarNoContent()
        {
            var dto = new EspecialidadeDTO { Nome = "Oncologia Clínica" };
            _svc.Setup(s => s.UpdateAsync(7, "Oncologia Clínica")).Returns(Task.CompletedTask);

            var result = await _controller.Update(7, dto);

            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.UpdateAsync(7, "Oncologia Clínica"), Times.Once);
        }

        [Fact]
        public async Task Update_Inexistente_DeveRetornarNotFound()
        {
            var dto = new EspecialidadeDTO { Nome = "X" };
            _svc.Setup(s => s.UpdateAsync(7, "X")).ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.Update(7, dto);

            Assert.IsType<NotFoundResult>(result);
            _svc.Verify(s => s.UpdateAsync(7, "X"), Times.Once);
        }

        [Fact]
        public async Task Update_NomeInvalido_DeveRetornarBadRequest()
        {
            var dto = new EspecialidadeDTO { Nome = "" };
            _svc.Setup(s => s.UpdateAsync(7, ""))
                .ThrowsAsync(new ArgumentException("Nome é obrigatório."));

            var result = await _controller.Update(7, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nome é obrigatório.", bad.Value);

            _svc.Verify(s => s.UpdateAsync(7, ""), Times.Once);
        }

        [Fact]
        public async Task Update_Duplicado_DeveRetornarConflict()
        {
            var dto = new EspecialidadeDTO { Nome = "Cardiologia" };
            _svc.Setup(s => s.UpdateAsync(7, "Cardiologia"))
                .ThrowsAsync(new InvalidOperationException("Já existe."));

            var result = await _controller.Update(7, dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Já existe.", conflict.Value);

            _svc.Verify(s => s.UpdateAsync(7, "Cardiologia"), Times.Once);
        }

        [Fact]
        public async Task Delete_Sucesso_DeveRetornarNoContent()
        {
            _svc.Setup(s => s.DeleteAsync(9)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(9);

            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync(9), Times.Once);
        }

        [Fact]
        public async Task Delete_Inexistente_DeveRetornarNotFound()
        {
            _svc.Setup(s => s.DeleteAsync(9))
                .ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.Delete(9);

            Assert.IsType<NotFoundResult>(result);
            _svc.Verify(s => s.DeleteAsync(9), Times.Once);
        }

        [Fact]
        public async Task Delete_ComConflito_DeveRetornarConflict()
        {
            _svc.Setup(s => s.DeleteAsync(9))
                .ThrowsAsync(new InvalidOperationException("Ligada a médicos."));

            var result = await _controller.Delete(9);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Ligada a médicos.", conflict.Value);

            _svc.Verify(s => s.DeleteAsync(9), Times.Once);
        }
    }
}
