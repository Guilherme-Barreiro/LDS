using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

using ConsultaPlus.Infrastructure.Services;
using ConsultaPlus.Core.Interfaces;
using MedicoModel = ConsultaPlus.Core.Models.Medico;
using static ConsultaPlus.Tests.TextAssert;

namespace ConsultaPlus.Tests.Medicos
{
    public class MedicoServiceTests
    {
        private readonly Mock<IMedicoRepository> _repo;
        private readonly MedicoService _svc;

        public MedicoServiceTests()
        {
            _repo = new Mock<IMedicoRepository>(MockBehavior.Strict);
            _svc = new MedicoService(_repo.Object);
        }

        [Fact]
        public async Task GetAll_DeveRetornarListaDoRepositorio()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new MedicoModel { Id = 1, NomeCompleto = "A" },
                new MedicoModel { Id = 2, NomeCompleto = "B" }
            }.AsEnumerable());

            var res = await _svc.GetAllAsync();

            Assert.Equal(2, res.Count());
            _repo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_DeveEncaminharParaRepositorio()
        {
            _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new MedicoModel { Id = 10, NomeCompleto = "Doc" });

            var m = await _svc.GetByIdAsync(10);

            Assert.NotNull(m);
            Assert.Equal(10, m!.Id);
            _repo.Verify(r => r.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task Search_DeveEncaminharParaRepositorio()
        {
            _repo.Setup(r => r.SearchByNameAsync("ana")).ReturnsAsync(Enumerable.Empty<MedicoModel>());

            var res = await _svc.SearchByNomeAsync("ana");

            Assert.Empty(res);
            _repo.Verify(r => r.SearchByNameAsync("ana"), Times.Once);
        }

        [Theory]
        [InlineData(null, "a@a.com", "U1", "NomeCompleto é obrigatório.")]
        [InlineData("Doc", null, "U1", "Email é obrigatório.")]
        [InlineData("Doc", "a@a.com", null, "NUtente é obrigatório.")]
        [InlineData("   ", "a@a.com", "U1", "NomeCompleto é obrigatório.")]
        public async Task Create_Invalido_DeveLancarArgumentException(string nome, string email, string nutente, string msg)
        {
            var novo = new MedicoModel
            {
                NomeCompleto = nome,
                Email = email,
                NUtente = nutente
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(novo));
            ContainsIgnoringDiacritics(msg, ex.Message);

            _repo.Verify(r => r.AddAsync(It.IsAny<MedicoModel>()), Times.Never);
        }

        [Fact]
        public async Task Create_Valido_DeveChamarRepoEDevolverEntidade()
        {
            var novo = new MedicoModel
            {
                NomeCompleto = "  Doc A ",
                Email = "  a@a.com ",
                NUtente = "  U1 ",
                Telemovel = "  911 ",
                DataNascimento = new DateTime(1990, 1, 1)
            };

            _repo.Setup(r => r.AddAsync(It.IsAny<MedicoModel>()))
                 .Callback<MedicoModel>(m => m.Id = 123)
                 .Returns(Task.CompletedTask);

            var res = await _svc.CreateAsync(novo);

            Assert.Same(novo, res);
            Assert.Equal(123, res.Id);
            _repo.Verify(r => r.AddAsync(novo), Times.Once);
        }

        [Fact]
        public async Task Update_Inexistente_DeveLancarKeyNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((MedicoModel?)null);

            var m = new MedicoModel { Id = 7, NomeCompleto = "X" };

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.UpdateAsync(m));
            ContainsIgnoringDiacritics("Medico 7 nao existe", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(7), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<MedicoModel>()), Times.Never);
        }

        [Fact]
        public async Task Update_Valido_DeveAplicarTrimEAtribuirCampos()
        {
            var existente = new MedicoModel
            {
                Id = 3,
                NomeCompleto = "Antigo",
                Email = "old@ex.com",
                Telemovel = "900",
                DataNascimento = new DateTime(1980, 1, 1)
            };

            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existente);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<MedicoModel>())).Returns(Task.CompletedTask);

            var entrada = new MedicoModel
            {
                Id = 3,
                NomeCompleto = "  Novo Nome ",
                Email = "  novo@ex.com ",
                Telemovel = "  933 ",
                DataNascimento = new DateTime(1985, 12, 31)
            };

            await _svc.UpdateAsync(entrada);

            _repo.Verify(r => r.UpdateAsync(It.Is<MedicoModel>(m =>
                m.Id == 3 &&
                m.NomeCompleto == "Novo Nome" &&
                m.Email == "novo@ex.com" &&
                m.Telemovel == "933" &&
                m.DataNascimento == new DateTime(1985, 12, 31)
            )), Times.Once);
        }

        [Fact]
        public async Task Update_Parciais_NaoDevemApagarCampos()
        {
            var existente = new MedicoModel
            {
                Id = 8,
                NomeCompleto = "Antigo",
                Email = "old@ex.com",
                Telemovel = "900",
                DataNascimento = new DateTime(1980, 1, 1)
            };

            _repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(existente);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<MedicoModel>())).Returns(Task.CompletedTask);

            var entrada = new MedicoModel
            {
                Id = 8,
                NomeCompleto = "  Novo  "
            };

            await _svc.UpdateAsync(entrada);

            _repo.Verify(r => r.UpdateAsync(It.Is<MedicoModel>(m =>
                m.Id == 8 &&
                m.NomeCompleto == "Novo" &&
                m.Email == "old@ex.com" &&
                m.Telemovel == "900" &&
                m.DataNascimento == new DateTime(1980, 1, 1)
            )), Times.Once);
        }

        [Fact]
        public async Task Delete_Inexistente_DeveLancarKeyNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((MedicoModel?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.DeleteAsync(9));
            ContainsIgnoringDiacritics("Medico 9 nao existe", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(9), Times.Once);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Sucesso_DeveChamarRepositorio()
        {
            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new MedicoModel { Id = 9 });
            _repo.Setup(r => r.DeleteAsync(9)).Returns(Task.CompletedTask);

            await _svc.DeleteAsync(9);

            _repo.Verify(r => r.GetByIdAsync(9), Times.Once);
            _repo.Verify(r => r.DeleteAsync(9), Times.Once);
        }
    }
}
