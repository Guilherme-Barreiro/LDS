using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Salas;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Core.Interfaces;

namespace ConsultaPlus.Tests.Controllers
{
    public class SalasControllerTests
    {
        private readonly Mock<ISalasService> _svc;
        private readonly SalasController _controller;

        public SalasControllerTests()
        {
            _svc = new Mock<ISalasService>(MockBehavior.Strict);
            _controller = new SalasController(_svc.Object);
        }

        [Fact]
        public async Task GetAll_DeveRetornarOk_ComListaMapeada()
        {
            var dados = new List<Sala>
            {
                new Sala { Id = 1, Nome = "Sala A" },
                new Sala { Id = 2, Nome = "Sala B" },
            };

            _svc.Setup(s => s.GetAllAsync()).ReturnsAsync(dados.AsEnumerable());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsAssignableFrom<IEnumerable<SalaResponseDto>>(ok.Value);
            Assert.Equal(2, body.Count());
            Assert.Contains(body, x => x.Id == 1 && x.Nome == "Sala A");
            Assert.Contains(body, x => x.Id == 2 && x.Nome == "Sala B");

            _svc.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_Existente_DeveRetornarOk()
        {
            _svc.Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(new Sala { Id = 10, Nome = "X" });

            var result = await _controller.GetById(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<SalaResponseDto>(ok.Value);
            Assert.Equal(10, dto.Id);
            Assert.Equal("X", dto.Nome);

            _svc.Verify(s => s.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetById_Inexistente_DeveRetornarNotFound()
        {
            _svc.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((Sala?)null);

            var result = await _controller.GetById(999);

            Assert.IsType<NotFoundResult>(result);
            _svc.Verify(s => s.GetByIdAsync(999), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchByNome_Invalido_DeveRetornarBadRequest(string? nome)
        {
            var result = await _controller.SearchByNome(nome!);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Parâmetro 'nome' é obrigatório para pesquisa.", bad.Value);

            _svc.Verify(s => s.SearchAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SearchByNome_Valido_DeveRetornarOk_ComLista()
        {
            var dados = new List<Sala>
            {
                new Sala { Id = 3, Nome = "Sala Azul" },
                new Sala { Id = 4, Nome = "Sala Azul Clara" },
            };

            _svc.Setup(s => s.SearchAsync("azul"))
                .ReturnsAsync(dados.AsEnumerable());

            var result = await _controller.SearchByNome("azul");

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsAssignableFrom<IEnumerable<SalaResponseDto>>(ok.Value);
            Assert.Equal(2, body.Count());
            Assert.Contains(body, s => s.Id == 3 && s.Nome == "Sala Azul");
            Assert.Contains(body, s => s.Id == 4 && s.Nome == "Sala Azul Clara");

            _svc.Verify(s => s.SearchAsync("azul"), Times.Once);
        }

        [Fact]
        public async Task SearchByNome_DeveEncaminharParametroTalComoVem()
        {
            _svc.Setup(s => s.SearchAsync(It.IsAny<string>()))
                .ReturnsAsync(Enumerable.Empty<Sala>());

            var result = await _controller.SearchByNome("  Sala  ");

            Assert.IsType<OkObjectResult>(result);
            _svc.Verify(s => s.SearchAsync("  Sala  "), Times.Once);
        }

        [Fact]
        public async Task Create_Valido_DeveRetornarCreatedAtAction()
        {
            var dto = new CreateSalaDto { Nome = "  Nova  " };
            _svc.Setup(s => s.CreateAsync("  Nova  ")).ReturnsAsync(42);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(SalasController.GetById), created.ActionName);
            Assert.Equal(42, created.RouteValues!["id"]);
            var body = Assert.IsType<SalaResponseDto>(created.Value);
            Assert.Equal(42, body.Id);
            Assert.Equal("Nova", body.Nome);

            _svc.Verify(s => s.CreateAsync("  Nova  "), Times.Once);
        }

        [Fact]
        public async Task Create_ArgumentException_DeveRetornarBadRequest()
        {
            var dto = new CreateSalaDto { Nome = "" };
            _svc.Setup(s => s.CreateAsync(""))
                .ThrowsAsync(new ArgumentException("Nome da sala é obrigatório."));

            var result = await _controller.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nome da sala é obrigatório.", bad.Value);
        }

        [Fact]
        public async Task Create_Conflict_DeveRetornar409()
        {
            var dto = new CreateSalaDto { Nome = "Sala A" };
            _svc.Setup(s => s.CreateAsync("Sala A"))
                .ThrowsAsync(new InvalidOperationException("Sala já existe."));

            var result = await _controller.Create(dto);

            var conf = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Sala já existe.", conf.Value);
        }

        [Fact]
        public async Task Delete_Sucesso_DeveRetornarNoContent()
        {
            _svc.Setup(s => s.DeleteAsync(7)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(7);

            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync(7), Times.Once);
        }

        [Fact]
        public async Task Delete_NotFound_DeveRetornar404()
        {
            _svc.Setup(s => s.DeleteAsync(7))
                .ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.Delete(7);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Conflict_DeveRetornar409()
        {
            _svc.Setup(s => s.DeleteAsync(7))
                .ThrowsAsync(new InvalidOperationException("Existe marcação para a sala."));

            var result = await _controller.Delete(7);

            var conf = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Existe marcação para a sala.", conf.Value);
        }
    }
}
