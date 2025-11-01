using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Medicos;
using ConsultaPlus.Core.Interfaces;
using MedicoModel = ConsultaPlus.Core.Models.Medico;

namespace ConsultaPlus.Tests.Medicos
{
    public class MedicosControllerTests
    {
        private readonly Mock<IMedicoRepository> _repo;
        private readonly MedicosController _controller;

        public MedicosControllerTests()
        {
            _repo = new Mock<IMedicoRepository>(MockBehavior.Strict);
            _controller = new MedicosController(_repo.Object);
        }

        [Fact]
        public async Task GetAll_DeveRetornarOk_ComListaMapeada()
        {
            // Arrange
            var dados = new List<MedicoModel>
            {
                new MedicoModel { Id = 1, NomeCompleto = "A", Telemovel = "911", Email = "a@a.com", NUtente = "U1", DataNascimento = new DateTime(1990,1,1), DataCriacao = new DateTime(2024,1,1) },
                new MedicoModel { Id = 2, NomeCompleto = "B", Telemovel = "922", Email = "b@b.com", NUtente = "U2", DataNascimento = new DateTime(1991,2,2), DataCriacao = new DateTime(2024,2,2) },
            };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(dados.AsEnumerable());

            // Act
            var result = await _controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<MedicoResponseDto>>(ok.Value);
            Assert.Equal(2, list.Count());
            Assert.Contains(list, x => x.Id == 1 && x.NomeCompleto == "A" && x.Email == "a@a.com" && x.NUtente == "U1");
            Assert.Contains(list, x => x.Id == 2 && x.NomeCompleto == "B" && x.Email == "b@b.com" && x.NUtente == "U2");
            _repo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_Existente_DeveRetornarOk()
        {
            var m = new MedicoModel { Id = 10, NomeCompleto = "Doc X", Telemovel = "933", Email = "x@x.com", NUtente = "UX", DataNascimento = new DateTime(1980, 5, 5) };
            _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(m);

            var result = await _controller.GetById(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<MedicoResponseDto>(ok.Value);
            Assert.Equal(10, dto.Id);
            Assert.Equal("Doc X", dto.NomeCompleto);
            Assert.Equal("933", dto.Telemovel);
            Assert.Equal("x@x.com", dto.Email);
            Assert.Equal("UX", dto.NUtente);
            _repo.Verify(r => r.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetById_Inexistente_DeveRetornarNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MedicoModel?)null);

            var result = await _controller.GetById(999);

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        [Theory]
        [InlineData(null, "U1", "a@a.com", "911", "NomeCompleto é obrigatório.")]
        [InlineData("Doc A", null, "a@a.com", "911", "NUtente é obrigatório.")]
        [InlineData("Doc A", "U1", null, "911", "Email é obrigatório.")]
        [InlineData("Doc A", "U1", "a@a.com", null, "Telemovel é obrigatório.")]
        [InlineData("   ", "U1", "a@a.com", "911", "NomeCompleto é obrigatório.")]
        [InlineData("Doc A", "   ", "a@a.com", "911", "NUtente é obrigatório.")]
        [InlineData("Doc A", "U1", "   ", "911", "Email é obrigatório.")]
        [InlineData("Doc A", "U1", "a@a.com", "   ", "Telemovel é obrigatório.")]
        public async Task Create_Invalido_DeveRetornarBadRequest(
            string nome, string nutente, string email, string telemovel, string expectedMsg)
        {
            var dto = new CreateMedicoDto
            {
                NomeCompleto = nome,
                NUtente = nutente,
                Email = email,
                Telemovel = telemovel,
                Password = "pwd",
                DataNascimento = new DateTime(1990, 1, 1)
            };

            var result = await _controller.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(expectedMsg, bad.Value);
            _repo.Verify(r => r.AddAsync(It.IsAny<MedicoModel>()), Times.Never);
        }

        [Fact]
        public async Task Create_Valido_DeveAdicionar_E_RetornarCreatedAtAction()
        {
            var dto = new CreateMedicoDto
            {
                NomeCompleto = "  Doc A  ",
                NUtente = "  U1 ",
                Email = "  a@a.com ",
                Telemovel = "  911 ",
                Password = "pwd",
                DataNascimento = new DateTime(1990, 1, 1)
            };

            _repo.Setup(r => r.AddAsync(It.IsAny<MedicoModel>()))
                 .Callback<MedicoModel>(m => m.Id = 42) // simula persistência
                 .Returns(Task.CompletedTask);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(MedicosController.GetById), created.ActionName);
            Assert.NotNull(created.RouteValues);
            Assert.True(created.RouteValues!.ContainsKey("id"));
            Assert.Equal(42, created.RouteValues["id"]);

            var body = Assert.IsType<MedicoResponseDto>(created.Value);
            Assert.Equal(42, body.Id);
            Assert.Equal("Doc A", body.NomeCompleto);        // trimmed
            Assert.Equal("911", body.Telemovel);             // trimmed
            Assert.Equal("a@a.com", body.Email);             // trimmed
            Assert.Equal("U1", body.NUtente);                // trimmed

            _repo.Verify(r => r.AddAsync(It.Is<MedicoModel>(m =>
                m.NomeCompleto == "Doc A" &&
                m.Telemovel == "911" &&
                m.Email == "a@a.com" &&
                m.NUtente == "U1" &&
                m.PasswordHash == "pwd" &&
                m.DataNascimento == new DateTime(1990, 1, 1)
            )), Times.Once);
        }

        [Fact]
        public async Task Update_Inexistente_DeveRetornarNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((MedicoModel?)null);

            var dto = new UpdateMedicoDto { NomeCompleto = "Novo" };

            var result = await _controller.Update(7, dto);

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.GetByIdAsync(7), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<MedicoModel>()), Times.Never);
        }

        [Fact]
        public async Task Update_Valido_DeveAtualizarCamposComTrim_E_RetornarNoContent()
        {
            var original = new MedicoModel
            {
                Id = 3,
                NomeCompleto = "Antigo",
                Telemovel = "900",
                Email = "old@ex.com",
                DataNascimento = new DateTime(1980, 1, 1)
            };

            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(original);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<MedicoModel>())).Returns(Task.CompletedTask);

            var dto = new UpdateMedicoDto
            {
                NomeCompleto = "  Novo Nome ",
                Telemovel = "  933 ",
                Email = "  novo@ex.com ",
                DataNascimento = new DateTime(1985, 12, 31)
            };

            var result = await _controller.Update(3, dto);

            Assert.IsType<NoContentResult>(result);
            _repo.Verify(r => r.UpdateAsync(It.Is<MedicoModel>(m =>
                m.Id == 3 &&
                m.NomeCompleto == "Novo Nome" &&
                m.Telemovel == "933" &&
                m.Email == "novo@ex.com" &&
                m.DataNascimento == new DateTime(1985, 12, 31)
            )), Times.Once);
        }

        [Fact]
        public async Task Delete_DeveInvocarRepo_ERetornarNoContent()
        {
            _repo.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(5);

            Assert.IsType<NoContentResult>(result);
            _repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Search_SemNome_DeveRetornarBadRequest(string? nome)
        {
            // Act
            var result = await _controller.Search(nome!);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            // o controller devolve um objeto { message = "..." }
            var prop = bad.Value!.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Parâmetro 'nome' é obrigatório.", prop!.GetValue(bad.Value)?.ToString());

            _repo.Verify(r => r.SearchByNameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Search_ComNome_DeveRetornarOk_ComListaMapeada()
        {
            // Arrange
            var dados = new List<MedicoModel>
    {
        new MedicoModel { Id = 1, NomeCompleto = "Ana Médica", Telemovel = "911", Email = "ana@ex.com", NUtente = "U1", DataNascimento = new DateTime(1990,1,1) },
        new MedicoModel { Id = 2, NomeCompleto = "Anabela",   Telemovel = "922", Email = "anabela@ex.com", NUtente = "U2", DataNascimento = new DateTime(1991,2,2) },
    };
            _repo.Setup(r => r.SearchByNameAsync("ana")).ReturnsAsync(dados.AsEnumerable());

            // Act
            var result = await _controller.Search("ana");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<MedicoResponseDto>>(ok.Value);
            Assert.Equal(2, list.Count());
            Assert.Contains(list, x => x.Id == 1 && x.NomeCompleto == "Ana Médica" && x.Email == "ana@ex.com" && x.NUtente == "U1");
            Assert.Contains(list, x => x.Id == 2 && x.NomeCompleto == "Anabela" && x.Email == "anabela@ex.com" && x.NUtente == "U2");

            _repo.Verify(r => r.SearchByNameAsync("ana"), Times.Once);
        }

        [Fact]
        public async Task Search_DeveEncaminharParametroAoRepositorio_SemAlterar()
        {
            // Arrange
            _repo.Setup(r => r.SearchByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync(Enumerable.Empty<MedicoModel>());

            // Act
            var result = await _controller.Search("  Ana  ");

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _repo.Verify(r => r.SearchByNameAsync("  Ana  "), Times.Once); // o controller não faz trim/normalize
        }

    }
}
