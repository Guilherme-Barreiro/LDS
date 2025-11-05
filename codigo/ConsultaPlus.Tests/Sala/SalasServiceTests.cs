using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

using ConsultaPlus.Infrastructure.Services;
using ConsultaPlus.Core.Interfaces;
using SalaModel = ConsultaPlus.Core.Models.Sala;

namespace ConsultaPlus.Tests.Salas
{
    public class SalasServiceTests
    {
        private readonly Mock<ISalaRepository> _repo;
        private readonly Mock<IUnitOfWork> _uow;
        private readonly SalasService _svc;

        public SalasServiceTests()
        {
            _repo = new Mock<ISalaRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _svc = new SalasService(_repo.Object, _uow.Object);
        }

        [Fact]
        public async Task GetAllAsync_DeveDevolverListaDoRepositorio()
        {
            var dados = new List<SalaModel>
            {
                new SalaModel { Id = 1, Nome = "A" },
                new SalaModel { Id = 2, Nome = "B" }
            };

            _repo.Setup(r => r.GetAllAsync())
                 .ReturnsAsync(dados.AsEnumerable());

            var res = await _svc.GetAllAsync();

            Assert.Equal(2, res.Count());
            _repo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_DeveEncaminharParaRepositorio()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                 .ReturnsAsync(new SalaModel { Id = 10, Nome = "X" });

            var sala = await _svc.GetByIdAsync(10);

            Assert.NotNull(sala);
            Assert.Equal(10, sala!.Id);
            _repo.Verify(r => r.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DeveEncaminharParametro()
        {
            _repo.Setup(r => r.SearchByNameAsync("abc"))
                 .ReturnsAsync(Enumerable.Empty<SalaModel>());

            var res = await _svc.SearchAsync("abc");

            Assert.Empty(res);
            _repo.Verify(r => r.SearchByNameAsync("abc"), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_NomeInvalido_DeveLancarArgumentException(string? nome)
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(nome!));
            Assert.Equal("Nome da sala é obrigatório.", ex.Message);

            _repo.Verify(r => r.ExistsByNameAsync(It.IsAny<string>()), Times.Never);
            _repo.Verify(r => r.AddAsync(It.IsAny<SalaModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NomeDuplicado_DeveLancarInvalidOperationException()
        {
            _repo.Setup(r => r.ExistsByNameAsync("Sala X")).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _svc.CreateAsync("  Sala X  ")
            );
            Assert.Equal("Já existe uma sala com esse nome.", ex.Message);

            _repo.Verify(r => r.ExistsByNameAsync("Sala X"), Times.Once);
            _repo.Verify(r => r.AddAsync(It.IsAny<SalaModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_Valido_DeveAdicionarGuardarERetornarId()
        {
            _repo.Setup(r => r.ExistsByNameAsync("Sala Nova")).ReturnsAsync(false);

            _repo.Setup(r => r.AddAsync(It.IsAny<SalaModel>()))
                 .Callback<SalaModel>(s => s.Id = 123) 
                 .Returns(Task.CompletedTask);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var id = await _svc.CreateAsync("  Sala Nova  ");

            Assert.Equal(123, id);

            _repo.Verify(r => r.ExistsByNameAsync("Sala Nova"), Times.Once);
            _repo.Verify(r => r.AddAsync(It.Is<SalaModel>(s => s.Nome == "Sala Nova")), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Inexistente_DeveLancarKeyNotFoundException()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((SalaModel?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.DeleteAsync(7));
            Assert.Equal("Sala não existe.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(7), Times.Once);
            _repo.Verify(r => r.HasFutureConsultasAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ComConsultasFuturas_DeveLancarInvalidOperationException()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new SalaModel { Id = 7, Nome = "S" });
            _repo.Setup(r => r.HasFutureConsultasAsync(7, It.IsAny<DateTime>())).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.DeleteAsync(7));
            Assert.Equal("Não é possível remover: a sala tem consultas futuras.", ex.Message);

            _repo.Verify(r => r.GetByIdAsync(7), Times.Once);
            _repo.Verify(r => r.HasFutureConsultasAsync(7, It.IsAny<DateTime>()), Times.Once);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Sucesso_DeveApagarESalvar()
        {
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new SalaModel { Id = 7, Nome = "S" });
            _repo.Setup(r => r.HasFutureConsultasAsync(7, It.IsAny<DateTime>())).ReturnsAsync(false);
            _repo.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _svc.DeleteAsync(7);

            _repo.Verify(r => r.GetByIdAsync(7), Times.Once);
            _repo.Verify(r => r.HasFutureConsultasAsync(7, It.IsAny<DateTime>()), Times.Once);
            _repo.Verify(r => r.DeleteAsync(7), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
