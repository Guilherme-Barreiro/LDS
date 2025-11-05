using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EspecialidadeMedicoModel = ConsultaPlus.Core.Models.EspecialidadeMedico;

namespace ConsultaPlus.Tests.EspecialidadeMedico
{
    public class EspecialidadeMedicoServiceTests
    {
        private readonly Mock<IEspecialidadeMedicoRepository> _repo;
        private readonly Mock<IEspecialidadeRepository> _especialidadeRepo;
        private readonly Mock<IMedicoRepository> _medicoRepo;
        private readonly Mock<IUnitOfWork> _uow;
        private readonly EspecialidadeMedicoService _svc;

        public EspecialidadeMedicoServiceTests()
        {
            _repo = new Mock<IEspecialidadeMedicoRepository>(MockBehavior.Strict);
            _especialidadeRepo = new Mock<IEspecialidadeRepository>(MockBehavior.Strict);
            _medicoRepo = new Mock<IMedicoRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _svc = new EspecialidadeMedicoService(_repo.Object, _especialidadeRepo.Object, _medicoRepo.Object, _uow.Object);
        }

        [Fact]
        public async Task GetMedicosByEspecialidadeIdAsync_DeveRetornarMedicosDoRepositorio()
        {
            var medicos = new List<Medico>
            {
                new Medico { Id = 1, NomeCompleto = "Dr. Goncalo" },
                new Medico { Id = 2, NomeCompleto = "Dra. Maria" }
            };
            _repo.Setup(r => r.GetMedicosByEspecialidadeIdAsync(1)).ReturnsAsync(medicos);

            var res = await _svc.GetMedicosByEspecialidadeIdAsync(1);

            Assert.NotNull(res);
            Assert.Equal(2, res.Count());
            Assert.Equal("Dr. Goncalo", res.First().NomeCompleto);
            _repo.Verify(r => r.GetMedicosByEspecialidadeIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetMedicosByEspecialidadeIdAsync_SemMedicos_DeveRetornarListaVazia()
        {
            _repo.Setup(r => r.GetMedicosByEspecialidadeIdAsync(1)).ReturnsAsync(Enumerable.Empty<Medico>());

            var res = await _svc.GetMedicosByEspecialidadeIdAsync(1);

            Assert.NotNull(res);
            Assert.Empty(res);
            _repo.Verify(r => r.GetMedicosByEspecialidadeIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetEspecialidadesByMedicoIdAsync_DeveRetornarEspecialidadesDoRepositorio()
        {
            var especialidades = new List<Especialidade>
            {
                new Especialidade { Id = 1, Nome = "Cardiologia" },
                new Especialidade { Id = 2, Nome = "Dermatologia" }
            };
            _repo.Setup(r => r.GetEspecialidadesByMedicoIdAsync(1)).ReturnsAsync(especialidades);

            var res = await _svc.GetEspecialidadesByMedicoIdAsync(1);

            Assert.NotNull(res);
            Assert.Equal(2, res.Count());
            Assert.Equal("Cardiologia", res.First().Nome);
            _repo.Verify(r => r.GetEspecialidadesByMedicoIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetEspecialidadesByMedicoIdAsync_SemEspecialidades_DeveRetornarListaVazia()
        {
            _repo.Setup(r => r.GetEspecialidadesByMedicoIdAsync(1)).ReturnsAsync(Enumerable.Empty<Especialidade>());

            var res = await _svc.GetEspecialidadesByMedicoIdAsync(1);

            Assert.NotNull(res);
            Assert.Empty(res);
            _repo.Verify(r => r.GetEspecialidadesByMedicoIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task AddAsync_Valido_DeveAdicionarAssociacao()
        {
            int medicoId = 1, especialidadeId = 1;

            _medicoRepo.Setup(m => m.ExistsAsync(medicoId)).ReturnsAsync(true);
            _especialidadeRepo.Setup(e => e.GetByIdAsync(especialidadeId)).ReturnsAsync(new Especialidade { Id = especialidadeId, Nome = "Pediatria" });
            _repo.Setup(r => r.ExistsAsync(medicoId, especialidadeId)).ReturnsAsync(false);
            _repo.Setup(r => r.AddAsync(It.IsAny<EspecialidadeMedicoModel>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await _svc.AddAsync(medicoId, especialidadeId);

            _medicoRepo.Verify(m => m.ExistsAsync(medicoId), Times.Once);
            _especialidadeRepo.Verify(e => e.GetByIdAsync(especialidadeId), Times.Once);
            _repo.Verify(r => r.ExistsAsync(medicoId, especialidadeId), Times.Once);
            _repo.Verify(r => r.AddAsync(It.IsAny<EspecialidadeMedicoModel>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_MedicoNaoExiste_DeveLancarKeyNotFoundException()
        {
            int medicoId = 999, especialidadeId = 1;

            _medicoRepo.Setup(m => m.ExistsAsync(medicoId)).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.AddAsync(medicoId, especialidadeId));
            Assert.Equal("Medico nao encontrado.", ex.Message);

            _medicoRepo.Verify(m => m.ExistsAsync(medicoId), Times.Once);
            _especialidadeRepo.Verify(e => e.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.AddAsync(It.IsAny<EspecialidadeMedicoModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_EspecialidadeNaoExiste_DeveLancarKeyNotFoundException()
        {
            int medicoId = 1, especialidadeId = 999;

            _medicoRepo.Setup(m => m.ExistsAsync(medicoId)).ReturnsAsync(true);
            _especialidadeRepo.Setup(e => e.GetByIdAsync(especialidadeId)).ReturnsAsync((Especialidade?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.AddAsync(medicoId, especialidadeId));
            Assert.Equal("Especialidade nao encontrada.", ex.Message);

            _medicoRepo.Verify(m => m.ExistsAsync(medicoId), Times.Once);
            _especialidadeRepo.Verify(e => e.GetByIdAsync(especialidadeId), Times.Once);
            _repo.Verify(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.AddAsync(It.IsAny<EspecialidadeMedicoModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_AssociacaoJaExiste_DeveLancarInvalidOperationException()
        {
            int medicoId = 1, especialidadeId = 1;

            _medicoRepo.Setup(m => m.ExistsAsync(medicoId)).ReturnsAsync(true);
            _especialidadeRepo.Setup(e => e.GetByIdAsync(especialidadeId)).ReturnsAsync(new Especialidade { Id = especialidadeId, Nome = "Pediatria" });
            _repo.Setup(r => r.ExistsAsync(medicoId, especialidadeId)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.AddAsync(medicoId, especialidadeId));
            Assert.Equal("Associacao ja existe.", ex.Message);

            _medicoRepo.Verify(m => m.ExistsAsync(medicoId), Times.Once);
            _especialidadeRepo.Verify(e => e.GetByIdAsync(especialidadeId), Times.Once);
            _repo.Verify(r => r.ExistsAsync(medicoId, especialidadeId), Times.Once);
            _repo.Verify(r => r.AddAsync(It.IsAny<EspecialidadeMedicoModel>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valido_DeveDeletarAssociacao()
        {
            int medicoId = 1, especialidadeId = 1;

            _repo.Setup(r => r.ExistsAsync(medicoId, especialidadeId)).ReturnsAsync(true);
            _repo.Setup(r => r.DeleteAsync(medicoId, especialidadeId)).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await _svc.DeleteAsync(medicoId, especialidadeId);

            _repo.Verify(r => r.ExistsAsync(medicoId, especialidadeId), Times.Once);
            _repo.Verify(r => r.DeleteAsync(medicoId, especialidadeId), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_AssociacaoNaoExiste_DeveLancarKeyNotFoundException()
        {
            int medicoId = 1, especialidadeId = 999;

            _repo.Setup(r => r.ExistsAsync(medicoId, especialidadeId)).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.DeleteAsync(medicoId, especialidadeId));
            Assert.Equal("Associacao nao encontrada.", ex.Message);

            _repo.Verify(r => r.ExistsAsync(medicoId, especialidadeId), Times.Once);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExistsAsync_DeveRetornarTrue_QuandoAssociacaoExiste()
        {
            int medicoId = 1, especialidadeId = 1;

            _repo.Setup(r => r.ExistsAsync(medicoId, especialidadeId)).ReturnsAsync(true);

            var result = await _svc.ExistsAsync(medicoId, especialidadeId);

            Assert.True(result);
            _repo.Verify(r => r.ExistsAsync(medicoId, especialidadeId), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_DeveRetornarFalse_QuandoAssociacaoNaoExiste()
        {
            int medicoId = 1, especialidadeId = 999;

            _repo.Setup(r => r.ExistsAsync(medicoId, especialidadeId)).ReturnsAsync(false);

            var result = await _svc.ExistsAsync(medicoId, especialidadeId);

            Assert.False(result);
            _repo.Verify(r => r.ExistsAsync(medicoId, especialidadeId), Times.Once);
        }
    }
}