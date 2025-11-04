using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.Tests; 
using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Especialidade;
using ConsultaPlus.Infrastructure.Services;
using EspecialidadeModel = ConsultaPlus.Core.Models.Especialidade;

namespace ConsultaPlus.Tests.Especialidades
{
    public class EspecialidadeControllerTests
    {
        private readonly Mock<IEspecialidadeService> _svc;
        private readonly EspecialidadeController _controller;

        public EspecialidadeControllerTests()
        {
            _svc = new Mock<IEspecialidadeService>(MockBehavior.Strict);
            _controller = new EspecialidadeController(_svc.Object);
        }

        // Helper: extrai o campo "message" de um anonymous object
        private static string GetMessage(object? value)
            => value?.GetType().GetProperty("message")?.GetValue(value)?.ToString() ?? string.Empty;

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
            var dto = Assert.IsType<ReadEspecialidadeDTO>(ok.Value);
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
            var message = GetMessage(nf.Value);
            TextAssert.ContainsIgnoringDiacritics("nao encontrada", message);

            _svc.Verify(s => s.GetByIdAsync(999), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetByNome_NomeInvalido_DeveRetornarBadRequest(string? nome)
        {
            var result = await _controller.Search(nome!);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(bad.Value);
            TextAssert.ContainsIgnoringDiacritics("Termo de pesquisa e obrigatorio.", message);

            _svc.Verify(s => s.SearchAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetByNome_SemResultados_DeveRetornarNotFound()
        {
            _svc.Setup(s => s.SearchAsync("derma"))
                .ReturnsAsync(Enumerable.Empty<EspecialidadeModel>());

            var result = await _controller.Search("derma");

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            var msg = GetMessage(nf.Value);
            TextAssert.ContainsIgnoringDiacritics("nenhuma especialidade", msg);

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

            var result = await _controller.Search("derma");

            var ok = Assert.IsType<OkObjectResult>(result);
            var lista = Assert.IsAssignableFrom<IEnumerable<ReadEspecialidadeDTO>>(ok.Value);
            var item = Assert.Single(lista);
            Assert.Equal(2, item.Id);
            Assert.Equal("Dermatologia", item.Nome);

            _svc.Verify(s => s.SearchAsync("derma"), Times.Once);
        }

        [Fact]
        public async Task RegistarEspecialidade_Sucesso_DeveRetornarCreatedAtAction()
        {
            var dto = new CreateEspecialidadeDTO { Nome = "  Neurologia  " };
            _svc.Setup(s => s.AddAsync("  Neurologia  ")).ReturnsAsync(42);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(EspecialidadeController.Search), created.ActionName);

            Assert.NotNull(created.RouteValues);
            Assert.True(created.RouteValues.ContainsKey("id"));
            Assert.Equal(42, created.RouteValues["id"]);

            var readDto = Assert.IsType<ReadEspecialidadeDTO>(created.Value);
            Assert.Equal(42, readDto.Id);
            Assert.Equal("Neurologia", readDto.Nome);

            _svc.Verify(s => s.AddAsync("  Neurologia  "), Times.Once);
        }

        [Fact]
        public async Task RegistarEspecialidade_NomeInvalido_DeveRetornarBadRequest()
        {
            var dto = new CreateEspecialidadeDTO { Nome = "   " };
            _svc.Setup(s => s.AddAsync("   "))
                .ThrowsAsync(new ArgumentException("Nome obrigatorio."));

            var result = await _controller.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(bad.Value);
            Assert.Equal("Nome obrigatorio.", message);

            _svc.Verify(s => s.AddAsync("   "), Times.Once);
        }

        [Fact]
        public async Task RegistarEspecialidade_Duplicado_DeveRetornarConflict()
        {
            var dto = new CreateEspecialidadeDTO { Nome = "Cardiologia" };
            _svc.Setup(s => s.AddAsync("Cardiologia"))
                .ThrowsAsync(new InvalidOperationException("Ja existe."));

            var result = await _controller.Create(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflict.Value);
            Assert.Equal("Ja existe.", message);

            _svc.Verify(s => s.AddAsync("Cardiologia"), Times.Once);
        }

        [Fact]
        public async Task Update_Sucesso_DeveRetornarNoContent()
        {
            var dto = new UpdateEspecialidadeDTO { Nome = "Oncologia Clínica" };
            _svc.Setup(s => s.UpdateAsync(7, "Oncologia Clínica")).Returns(Task.CompletedTask);

            var result = await _controller.Update(7, dto);

            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.UpdateAsync(7, "Oncologia Clínica"), Times.Once);
        }

        [Fact]
        public async Task Update_Inexistente_DeveRetornarNotFound()
        {
            var dto = new UpdateEspecialidadeDTO { Nome = "X" };
            _svc.Setup(s => s.UpdateAsync(7, "X")).ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.Update(7, dto);

            Assert.IsType<NotFoundObjectResult>(result);
            _svc.Verify(s => s.UpdateAsync(7, "X"), Times.Once);
        }

        [Fact]
        public async Task Update_NomeInvalido_DeveRetornarBadRequest()
        {
            var dto = new UpdateEspecialidadeDTO { Nome = "" };
            _svc.Setup(s => s.UpdateAsync(7, ""))
                .ThrowsAsync(new ArgumentException("Nome obrigatorio."));

            var result = await _controller.Update(7, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var message = GetMessage(bad.Value);
            Assert.Equal("Nome obrigatorio.", message);

            _svc.Verify(s => s.UpdateAsync(7, ""), Times.Once);
        }

        [Fact]
        public async Task Update_Duplicado_DeveRetornarConflict()
        {
            var dto = new UpdateEspecialidadeDTO { Nome = "Cardiologia" };
            _svc.Setup(s => s.UpdateAsync(7, "Cardiologia"))
                .ThrowsAsync(new InvalidOperationException("Ja existe."));

            var result = await _controller.Update(7, dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflict.Value);
            Assert.Equal("Ja existe.", message);

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

            Assert.IsType<NotFoundObjectResult>(result);
            _svc.Verify(s => s.DeleteAsync(9), Times.Once);
        }

        [Fact]
        public async Task Delete_ComConflito_DeveRetornarConflict()
        {
            _svc.Setup(s => s.DeleteAsync(9))
                .ThrowsAsync(new InvalidOperationException("Nao e possível excluir a especialidade porque existem medicos vinculados."));

            var result = await _controller.Delete(9);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var message = GetMessage(conflict.Value);
            Assert.Equal("Nao e possível excluir a especialidade porque existem medicos vinculados.", message);

            _svc.Verify(s => s.DeleteAsync(9), Times.Once);
        }
    }
}
