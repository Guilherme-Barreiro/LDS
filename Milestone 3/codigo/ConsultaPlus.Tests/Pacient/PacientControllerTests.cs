using System.Net;
using System.Threading.Tasks;
using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Pacientes;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ConsultaPlus.Tests.Pacientes
{
    public class PacientesControllerTests
    {
        private readonly Mock<IPacienteRepository> _repo = new();
        private PacientesController CreateSut() => new(_repo.Object);

        [Fact]
        public async Task GetAll_Retorna200_ComLista()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Paciente { Id = 1, NUtente = "U1" },
                new Paciente { Id = 2, NUtente = "U2" },
            });

            var sut = CreateSut();
            var res = await sut.GetAll() as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal((int)HttpStatusCode.OK, res!.StatusCode);
        }

        [Fact]
        public async Task GetById_200_QuandoExiste()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                 .ReturnsAsync(new Paciente { Id = 10, NUtente = "U10" });

            var sut = CreateSut();
            var res = await sut.GetById(10) as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal((int)HttpStatusCode.OK, res!.StatusCode);
        }

        [Fact]
        public async Task GetById_404_QuandoNaoExiste()
        {
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Paciente?)null);

            var sut = CreateSut();
            var res = await sut.GetById(999) as StatusCodeResult;

            Assert.NotNull(res);
            Assert.Equal((int)HttpStatusCode.NotFound, res!.StatusCode);
        }

        [Fact]
        public async Task Create_400_QuandoNUtenteOuPasswordVazios()
        {
            var sut = CreateSut();

            var bad1 = await sut.Create(new CreatePacienteDto { NUtente = "", Password = "pwd" }) as ObjectResult;
            var bad2 = await sut.Create(new CreatePacienteDto { NUtente = "U1", Password = "" }) as ObjectResult;

            Assert.Equal((int)HttpStatusCode.BadRequest, bad1!.StatusCode);
            Assert.Equal((int)HttpStatusCode.BadRequest, bad2!.StatusCode);
            _repo.Verify(r => r.AddAsync(It.IsAny<Paciente>()), Times.Never);
        }

        [Fact]
        public async Task Create_409_QuandoDuplicado()
        {
            _repo.Setup(r => r.GetByNUtenteAsync("U1")).ReturnsAsync(new Paciente { Id = 5, NUtente = "U1" });

            var sut = CreateSut();
            var dto = new CreatePacienteDto { NUtente = " U1 ", Password = "pwd" };

            var res = await sut.Create(dto) as ObjectResult;

            Assert.Equal((int)HttpStatusCode.Conflict, res!.StatusCode);
            _repo.Verify(r => r.AddAsync(It.IsAny<Paciente>()), Times.Never);
        }

        [Fact]
        public async Task Create_201_ComTrim_E_Persiste()
        {
            _repo.Setup(r => r.GetByNUtenteAsync("U1")).ReturnsAsync((Paciente?)null);
            _repo.Setup(r => r.AddAsync(It.IsAny<Paciente>()))
                 .Callback<Paciente>(p => p.Id = 123)
                 .Returns(Task.CompletedTask);

            var sut = CreateSut();
            var dto = new CreatePacienteDto
            {
                NUtente = "  U1 ",
                Password = "pwd",
                NomeCompleto = "  Ana ",
                Email = "  a@a.com ",
                Telemovel = "  911 ",
                Morada = "  Rua ",
                Nif = "  123 "
            };

            var res = await sut.Create(dto) as CreatedAtActionResult;

            Assert.NotNull(res);
            Assert.Equal(nameof(PacientesController.GetById), res!.ActionName);
            var body = Assert.IsType<PacienteResponseDto>(res.Value);
            Assert.Equal(123, body.Id);
            Assert.Equal("U1", body.NUtente);
            Assert.Equal("Ana", body.NomeCompleto);
            Assert.Equal("a@a.com", body.Email);
            Assert.Equal("911", body.Telemovel);
            Assert.Equal("Rua", body.Morada);
            Assert.Equal("123", body.Nif);
            _repo.Verify(r => r.AddAsync(It.Is<Paciente>(p => p.PasswordHash != null && p.PasswordHash.Length > 0)), Times.Once);
        }

        [Fact]
        public async Task Update_404_QuandoNaoExiste()
        {
            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Paciente?)null);

            var sut = CreateSut();
            var res = await sut.Update(9, new UpdatePacienteDto()) as StatusCodeResult;

            Assert.Equal((int)HttpStatusCode.NotFound, res!.StatusCode);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Paciente>()), Times.Never);
        }

        [Fact]
        public async Task Update_204_QuandoExiste_E_AplicaCampos()
        {
            var pac = new Paciente { Id = 7, NUtente = "U7", NomeCompleto = "Antigo" };
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(pac);

            var sut = CreateSut();
            var dto = new UpdatePacienteDto { NomeCompleto = "Novo", Email = "novo@x.com" };

            var res = await sut.Update(7, dto) as StatusCodeResult;

            Assert.Equal((int)HttpStatusCode.NoContent, res!.StatusCode);
            Assert.Equal("Novo", pac.NomeCompleto);
            Assert.Equal("novo@x.com", pac.Email);
            _repo.Verify(r => r.UpdateAsync(pac), Times.Once);
        }

        [Fact]
        public async Task Delete_204_SempreChamaRepo()
        {
            var sut = CreateSut();
            var res = await sut.Delete(42) as StatusCodeResult;

            Assert.Equal((int)HttpStatusCode.NoContent, res!.StatusCode);
            _repo.Verify(r => r.DeleteAsync(42), Times.Once);
        }
    }
}
