using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Salas;
using ConsultaPlus.Core.Interfaces;
using SalaModel = ConsultaPlus.Core.Models.Sala;

namespace ConsultaPlus.Tests.Controllers
{
    public class SalasControllerTests
    {
        private readonly Mock<ISalaRepository> _repoMock;
        private readonly SalasController _controller;

        public SalasControllerTests()
        {
            _repoMock = new Mock<ISalaRepository>(MockBehavior.Strict);
            _controller = new SalasController(_repoMock.Object);
        }

        [Fact]
        public async Task GetAll_DeveRetornarOk_ComListaDeSalaResponseDto()
        {
            // Arrange
            var dados = new List<SalaModel>
            {
                new SalaModel { Id = 1, Nome = "Sala A" },
                new SalaModel { Id = 2, Nome = "Sala B" }
            };
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(dados.AsEnumerable());

            // Act
            var result = await _controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsAssignableFrom<IEnumerable<SalaResponseDto>>(ok.Value);
            Assert.Equal(2, body.Count());
            Assert.Contains(body, s => s.Id == 1 && s.Nome == "Sala A");
            Assert.Contains(body, s => s.Id == 2 && s.Nome == "Sala B");

            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_Existente_DeveRetornarOk_ComSalaResponseDto()
        {
            // Arrange
            var sala = new SalaModel { Id = 10, Nome = "Sala X" };
            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(sala);

            // Act
            var result = await _controller.GetById(10);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<SalaResponseDto>(ok.Value);
            Assert.Equal(10, body.Id);
            Assert.Equal("Sala X", body.Nome);

            _repoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetById_Inexistente_DeveRetornarNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((SalaModel?)null);

            // Act
            var result = await _controller.GetById(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _repoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_NomeInvalido_DeveRetornarBadRequest(string nomeInvalido)
        {
            // Arrange
            var dto = new CreateSalaDto { Nome = nomeInvalido };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nome da sala é obrigatório.", bad.Value);

            _repoMock.Verify(r => r.AddAsync(It.IsAny<SalaModel>()), Times.Never);
        }

        [Fact]
        public async Task Create_Valido_DeveChamarRepoAddAsync_E_ReturnarCreatedAtAction()
        {
            // Arrange
            var dto = new CreateSalaDto { Nome = "  Sala Nova  " };
            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<SalaModel>()))
                .Callback<SalaModel>(s => s.Id = 42) // simula atribuição de Id
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(SalasController.GetById), created.ActionName);

            // route values
            Assert.NotNull(created.RouteValues);
            Assert.True(created.RouteValues!.ContainsKey("id"));
            Assert.Equal(42, created.RouteValues["id"]);

            // body
            var body = Assert.IsType<SalaResponseDto>(created.Value);
            Assert.Equal(42, body.Id);
            Assert.Equal("Sala Nova", body.Nome);

            _repoMock.Verify(r => r.AddAsync(It.Is<SalaModel>(s => s.Nome == "Sala Nova")), Times.Once);
        }

        [Fact]
        public async Task Delete_DeveInvocarRepo_ERetornarNoContent()
        {
            // Arrange
            _repoMock.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(7);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _repoMock.Verify(r => r.DeleteAsync(7), Times.Once);
        }
    }
}
