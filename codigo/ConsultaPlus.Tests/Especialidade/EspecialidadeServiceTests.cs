using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Repositories;
using ConsultaPlus.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EspecialidadeModel = ConsultaPlus.Core.Models.Especialidade;

namespace ConsultaPlus.Tests.Especialidades
{
    public class EspecialidadeServiceTests
    {
        private readonly Mock<IEspecialidadeRepository> _repo;
        private readonly Mock<IUnitOfWork> _uow;
        private readonly EspecialidadeService _svc;

        public EspecialidadeServiceTests()
        {
            _repo = new Mock<IEspecialidadeRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _svc = new EspecialidadeService(_repo.Object, _uow.Object);
        }

        [Fact]
        public async Task GetAllAsync_DeveRetornarListaDoRepositorio()
        {
            var dados = new List<EspecialidadeModel>
            {
                new EspecialidadeModel { Id = 1, Nome = "Cardiologia" },
                new EspecialidadeModel { Id = 2, Nome = "Dermatologia" }
            };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(dados.AsEnumerable());

            var res = await _svc.GetAllAsync();

            Assert.Equal(2, res.Count());
            _repo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_DeveEncaminharParaRepositorio()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                 .ReturnsAsync(new EspecialidadeModel { Id = 10, Nome = "Ortopedia" });

            var e = await _svc.GetByIdAsync(10);

            Assert.NotNull(e);
            Assert.Equal(10, e!.Id);
            Assert.Equal("Ortopedia", e.Nome);
            _repo.Verify(r => r.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DeveChamarSearchByNameAsync()
        {
            var especialidades = new List<EspecialidadeModel>
            {
                new EspecialidadeModel { Id = 1, Nome = "Neurologia" }
            };

            _repo.Setup(r => r.SearchByNameAsync("neuro"))
                 .ReturnsAsync(especialidades);

            var resultado = await _svc.SearchAsync("neuro");

            Assert.Single(resultado);
            _repo.Verify(r => r.SearchByNameAsync("neuro"), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DeveFiltrarPorSubstringCaseInsensitive()
        {
            var dados = new List<EspecialidadeModel>
            {
                new EspecialidadeModel { Id = 1, Nome = "Cardiologia" },
                new EspecialidadeModel { Id = 2, Nome = "Dermatologia" },
                new EspecialidadeModel { Id = 3, Nome = "Cirurgia" }
            };
            _repo.Setup(r => r.SearchByNameAsync("derma"))
         .ReturnsAsync(dados.Where(e => e.Nome.ToLower().Contains("derma")).AsEnumerable());

            var res = await _svc.SearchAsync("derma");

            var list = res.ToList();
            Assert.Single(list);
            Assert.Equal(2, list[0].Id);
            Assert.Equal("Dermatologia", list[0].Nome);

            _repo.Verify(r => r.SearchByNameAsync("derma"), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddAsync_NomeInvalido_DeveLancarArgumentException(string? nome)
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.AddAsync(nome!));
            Assert.Equal("Nome obrigatório.", ex.Message);

            _repo.Verify(r => r.SearchByNameAsync(It.IsAny<string>()), Times.Never);
            _repo.Verify(r => r.AddAsync(It.IsAny<EspecialidadeModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_NomeDuplicado_DeveLancarInvalidOperationException()
        {
            _repo.Setup(r => r.ExistsByNameAsync("Cardiologia")).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _svc.AddAsync("  Cardiologia  ")
            );
            Assert.Equal("Já existe uma especialidade com esse nome.", ex.Message);

            _repo.Verify(r => r.ExistsByNameAsync("Cardiologia"), Times.Once);

            _repo.Verify(r => r.AddAsync(It.IsAny<EspecialidadeModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_Valido_DeveAdicionar_Salvar_E_RetornarId()
        {
            _repo.Setup(r => r.ExistsByNameAsync("Neurologia")).ReturnsAsync(false);
            _repo.Setup(r => r.AddAsync(It.IsAny<EspecialidadeModel>()))
                 .Callback<EspecialidadeModel>(e => e.Id = 42)
                 .Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var id = await _svc.AddAsync("  Neurologia ");

            Assert.Equal(42, id);
            _repo.Verify(r => r.ExistsByNameAsync("Neurologia"), Times.Once);
            _repo.Verify(r => r.AddAsync(It.Is<EspecialidadeModel>(e => e.Nome == "Neurologia")), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Inexistente_DeveLancarKeyNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((EspecialidadeModel?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.UpdateAsync(7, "Novo"));
            Assert.Equal("Especialidade não encontrada.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(7), Times.Once);
            _repo.Verify(r => r.SearchByNameAsync(It.IsAny<string>()), Times.Never);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<EspecialidadeModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateAsync_NomeInvalido_DeveLancarArgumentException(string? novoNome)
        {
            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new EspecialidadeModel { Id = 3, Nome = "Gastro" });

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.UpdateAsync(3, novoNome!));
            Assert.Equal("Nome obrigatório.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.ExistsByNameAndNotIdAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<EspecialidadeModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_NomeDuplicado_DeveLancarInvalidOperationException()
        {
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new EspecialidadeModel { Id = 5, Nome = "Geriatria" });
            _repo.Setup(r => r.ExistsByNameAndNotIdAsync("Cardiologia", 5)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.UpdateAsync(5, "Cardiologia"));
            Assert.Equal("Já existe uma especialidade com esse nome.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(5), Times.Once);
            _repo.Verify(r => r.ExistsByNameAndNotIdAsync("Cardiologia", 5), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<EspecialidadeModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_Valido_DeveAtualizar_Salvar()
        {
            var original = new EspecialidadeModel { Id = 9, Nome = "Oncologia" };
            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(original);
            _repo.Setup(r => r.ExistsByNameAndNotIdAsync("Oncologia Clínica", 9)).ReturnsAsync(false);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<EspecialidadeModel>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await _svc.UpdateAsync(9, "  Oncologia Clínica ");

            _repo.Verify(r => r.UpdateAsync(It.Is<EspecialidadeModel>(e =>
                e.Id == 9 && e.Nome == "Oncologia Clínica"
            )), Times.Once);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Inexistente_DeveLancarKeyNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync((EspecialidadeModel?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.DeleteAsync(11));
            Assert.Equal("Especialidade não encontrada.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(11), Times.Once);
            _repo.Verify(r => r.IsLinkedToMedic(It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ComMedicosAssociados_DeveLancarInvalidOperationException()
        {
            _repo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new EspecialidadeModel { Id = 11, Nome = "Pediatria" });
            _repo.Setup(r => r.IsLinkedToMedic(11)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.DeleteAsync(11));
            Assert.Equal("Não é possível excluir a especialidade porque existem médicos vinculados.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(11), Times.Once);
            _repo.Verify(r => r.IsLinkedToMedic(11), Times.Once);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Sucesso_DeveApagarESalvar()
        {
            _repo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new EspecialidadeModel { Id = 11, Nome = "Pediatria" });
            _repo.Setup(r => r.IsLinkedToMedic(11)).ReturnsAsync(false);
            _repo.Setup(r => r.DeleteAsync(11)).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await _svc.DeleteAsync(11);

            _repo.Verify(r => r.DeleteAsync(11), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
