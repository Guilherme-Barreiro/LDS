using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

using ConsultaPlus.Infrastructure.Services;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using static ConsultaPlus.Tests.TextAssert; // se tiveres o helper de acentos

namespace ConsultaPlus.Tests.Consultas
{
    public class ConsultaServiceTests
    {
        private readonly Mock<IConsultaRepository> _consultas;
        private readonly Mock<IMedicoRepository> _medicos;
        private readonly Mock<IPacienteRepository> _pacientes;
        private readonly Mock<ISalaRepository> _salas;
        private readonly Mock<IEspecialidadeCRUD> _especialidades;

        private readonly ConsultaService _svc;

        public ConsultaServiceTests()
        {
            _consultas = new Mock<IConsultaRepository>(MockBehavior.Strict);
            _medicos = new Mock<IMedicoRepository>(MockBehavior.Strict);
            _pacientes = new Mock<IPacienteRepository>(MockBehavior.Strict);
            _salas = new Mock<ISalaRepository>(MockBehavior.Strict);
            _especialidades = new Mock<IEspecialidadeCRUD>(MockBehavior.Strict);

            _svc = new ConsultaService(_consultas.Object, _medicos.Object, _pacientes.Object, _salas.Object, _especialidades.Object);
        }

        [Fact]
        public async Task GetAll_DeveEncaminharParaRepositorio()
        {
            _consultas.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Consulta { Id = 1 }, new Consulta { Id = 2 }
            }.AsEnumerable());

            var res = await _svc.GetAllAsync();

            Assert.Equal(2, res.Count());
            _consultas.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_DeveEncaminharParaRepositorio()
        {
            _consultas.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Consulta { Id = 7 });

            var c = await _svc.GetByIdAsync(7);

            Assert.NotNull(c);
            Assert.Equal(7, c!.Id);
            _consultas.Verify(r => r.GetByIdAsync(7), Times.Once);
        }

        [Fact]
        public async Task GetByMedico_DeveFiltrar()
        {
            var list = new[]
            {
                new Consulta { Id = 1, MedicoId = 5 },
                new Consulta { Id = 2, MedicoId = 5 },
                new Consulta { Id = 3, MedicoId = 9 },
            };
            _consultas.Setup(r => r.GetAllAsync()).ReturnsAsync(list.AsEnumerable());

            var res = await _svc.GetByMedicoAsync(5);

            Assert.Equal(2, res.Count());
            Assert.All(res, x => Assert.Equal(5, x.MedicoId));
            _consultas.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByPaciente_DeveFiltrar()
        {
            var list = new[]
            {
                new Consulta { Id = 1, PacienteId = 10 },
                new Consulta { Id = 2, PacienteId = 10 },
                new Consulta { Id = 3, PacienteId = 99 },
            };
            _consultas.Setup(r => r.GetAllAsync()).ReturnsAsync(list.AsEnumerable());

            var res = await _svc.GetByPacienteAsync(10);

            Assert.Equal(2, res.Count());
            Assert.All(res, x => Assert.Equal(10, x.PacienteId));
            _consultas.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Create_PacienteInexistente_DeveLancar()
        {
            var nova = new Consulta { PacienteId = 1, MedicoId = 2, SalaId = 3, EspecialidadeId = 4 };

            _pacientes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Paciente?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(nova));
            ContainsIgnoringDiacritics("PacienteId 1 nao existe", ex.Message);

            _pacientes.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Create_MedicoInexistente_DeveLancar()
        {
            var nova = new Consulta { PacienteId = 1, MedicoId = 2, SalaId = 3, EspecialidadeId = 4 };

            _pacientes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Paciente { Id = 1 });
            _medicos.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Medico?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(nova));
            ContainsIgnoringDiacritics("MedicoId 2 nao existe", ex.Message);

            _pacientes.Verify(r => r.GetByIdAsync(1), Times.Once);
            _medicos.Verify(r => r.GetByIdAsync(2), Times.Once);
        }

        [Fact]
        public async Task Create_SalaInexistente_DeveLancar()
        {
            var nova = new Consulta { PacienteId = 1, MedicoId = 2, SalaId = 3, EspecialidadeId = 4 };

            _pacientes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Paciente { Id = 1 });
            _medicos.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Medico { Id = 2 });
            _salas.Setup(r => r.GetByIdAsync(3)).ReturnsAsync((Sala?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(nova));
            ContainsIgnoringDiacritics("SalaId 3 nao existe", ex.Message);

            _salas.Verify(r => r.GetByIdAsync(3), Times.Once);
        }

        [Fact]
        public async Task Create_EspecialidadeInexistente_DeveLancar()
        {
            var nova = new Consulta { PacienteId = 1, MedicoId = 2, SalaId = 3, EspecialidadeId = 4 };

            _pacientes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Paciente { Id = 1 });
            _medicos.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Medico { Id = 2 });
            _salas.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Sala { Id = 3, Nome = "S1" });
            _especialidades.Setup(r => r.GetByIdAsync(4)).ReturnsAsync((Especialidade?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(nova));
            ContainsIgnoringDiacritics("EspecialidadeId 4 nao existe", ex.Message);

            _especialidades.Verify(r => r.GetByIdAsync(4), Times.Once);
        }

        [Fact]
        public async Task Create_Sucesso_DeveAdicionarERetornarEntidade()
        {
            var dt = new DateTime(2025, 01, 10, 9, 0, 0, DateTimeKind.Utc);
            var nova = new Consulta
            {
                PacienteId = 1,
                MedicoId = 2,
                SalaId = 3,
                EspecialidadeId = 4,
                DataConsulta = dt,
                Duracao = 30,
                Estado = "Marcada"
            };

            _pacientes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Paciente { Id = 1 });
            _medicos.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Medico { Id = 2 });
            _salas.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Sala { Id = 3, Nome = "S1" });
            _especialidades.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Especialidade { Id = 4, Nome = "Cardio" });
            _consultas.Setup(r => r.AddAsync(It.IsAny<Consulta>()))
                          .Callback<Consulta>(c => c.Id = 99)
                          .Returns(Task.CompletedTask);

            var res = await _svc.CreateAsync(nova);

            Assert.Same(nova, res);
            Assert.Equal(99, res.Id);

            _consultas.Verify(r => r.AddAsync(It.Is<Consulta>(c =>
                c.PacienteId == 1 && c.MedicoId == 2 && c.SalaId == 3 &&
                c.EspecialidadeId == 4 && c.DataConsulta == dt &&
                c.Duracao == 30 && c.Estado == "Marcada"
            )), Times.Once);
        }

        [Fact]
        public async Task Delete_Inexistente_DeveLancar()
        {
            _consultas.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Consulta?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.DeleteAsync(9));
            ContainsIgnoringDiacritics("Consulta 9 nao existe", ex.Message);

            _consultas.Verify(r => r.GetByIdAsync(9), Times.Once);
            _consultas.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Sucesso_DeveChamarRepositorio()
        {
            _consultas.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new Consulta { Id = 9 });
            _consultas.Setup(r => r.DeleteAsync(9)).Returns(Task.CompletedTask);

            await _svc.DeleteAsync(9);

            _consultas.Verify(r => r.GetByIdAsync(9), Times.Once);
            _consultas.Verify(r => r.DeleteAsync(9), Times.Once);
        }
    }
}
