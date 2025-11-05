using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Sns;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Tests.SNS
{
    public class SnsPacientesControllerTests
    {
        private static Mock<ApplicationDbContext> MakeDb(
            IEnumerable<SnsPaciente>? snsSeed = null,
            IEnumerable<Paciente>? pacSeed = null)
        {
            snsSeed ??= Array.Empty<SnsPaciente>();
            pacSeed ??= Array.Empty<Paciente>();

            var dbMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            // DbSets com LINQ async prontos (AnyAsync, FirstOrDefaultAsync, ToListAsync, etc.)
            dbMock.Setup(x => x.SnsPacientes).ReturnsDbSet(snsSeed);
            dbMock.Setup(x => x.Pacientes).ReturnsDbSet(pacSeed);
            // SaveChangesAsync default
            dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            return dbMock;
        }

        // ============================= CREATE =============================

        [Fact]
        public async Task Create_TodosCamposValidos_201_ComTrim()
        {
            // Arrange
            var db = MakeDb(); // sem registos
            var added = new List<SnsPaciente>();

            // Intercepta Add para simular PK/Identity
            db.Setup(x => x.SnsPacientes.Add(It.IsAny<SnsPaciente>()))
              .Callback((SnsPaciente e) =>
              {
                  e.Id = 123;
                  added.Add(e);
              });

            var sut = new SnsPacientesController(db.Object);

            var dto = new CreateSnsPacienteDto
            {
                NUtente = "  U1  ",
                NomeCompleto = "  João Silva ",
                Nif = " 123456789 ",
                Telemovel = " 900000000 ",
                Morada = " Rua X ",
                Email = " joao@x.com ",
                DataNascimento = new DateTime(1990, 1, 1)
            };

            // Act
            var res = await sut.Create(dto, CancellationToken.None);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(res);
            Assert.Equal(nameof(SnsPacientesController.GetById), created.ActionName);
            Assert.Equal(123, (int)created.RouteValues!["id"]!);

            var body = Assert.IsType<SnsPacienteResponseDto>(created.Value);
            Assert.Equal("U1", body.NUtente);
            Assert.Equal("João Silva", body.NomeCompleto);
            Assert.Equal("123456789", body.Nif);
            Assert.Equal("900000000", body.Telemovel);
            Assert.Equal("Rua X", body.Morada);
            Assert.Equal("joao@x.com", body.Email);

            Assert.Single(added);
        }

        [Theory]
        [InlineData(null!, "a", "a", "a", "a", "a")]
        [InlineData("a", null!, "a", "a", "a", "a")]
        [InlineData("a", "a", null!, "a", "a", "a")]
        [InlineData("a", "a", "a", null!, "a", "a")]
        [InlineData("a", "a", "a", "a", null!, "a")]
        [InlineData("a", "a", "a", "a", "a", null!)]
        [InlineData("  ", "a", "a", "a", "a", "a")]
        public async Task Create_CampoVazio_400(string n, string nome, string nif, string tel, string mor, string email)
        {
            var db = MakeDb();
            var sut = new SnsPacientesController(db.Object);

            var dto = new CreateSnsPacienteDto
            {
                NUtente = n,
                NomeCompleto = nome,
                Nif = nif,
                Telemovel = tel,
                Morada = mor,
                Email = email,
                DataNascimento = new DateTime(2000, 1, 1)
            };

            var res = await sut.Create(dto, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var msg = bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!.ToString();
            Assert.Contains("obrigatórios", msg!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Create_DuplicadoNUtente_409()
        {
            var seed = new[]
            {
                new SnsPaciente { Id = 1, NUtente = "U1", NomeCompleto="X", Nif="1", Telemovel="9", Morada="M", Email="x@x", DataNascimento=new DateTime(1990,1,1) }
            };
            var db = MakeDb(snsSeed: seed);
            var sut = new SnsPacientesController(db.Object);

            var dto = new CreateSnsPacienteDto
            {
                NUtente = "  U1  ",
                NomeCompleto = "A",
                Nif = "1",
                Telemovel = "9",
                Morada = "M",
                Email = "a@a",
                DataNascimento = new DateTime(1990, 1, 1)
            };

            var res = await sut.Create(dto, CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(res);
            var msg = conflict.Value!.GetType().GetProperty("message")!.GetValue(conflict.Value)!.ToString();
            Assert.Contains("Já existe", msg);
        }

        // =================== GET ALL / BY ID / BY NUTENTE ===================

        [Fact]
        public async Task GetAll_200_ComLista()
        {
            var seed = new[]
            {
                new SnsPaciente { Id = 1, NUtente = "A", NomeCompleto = "AA", Nif = "1", Telemovel = "9", Morada = "M", Email = "a@a", DataNascimento = new DateTime(1990, 1, 1) },
                new SnsPaciente { Id = 2, NUtente = "B", NomeCompleto = "BB", Nif = "2", Telemovel = "8", Morada = "N", Email = "b@b", DataNascimento = new DateTime(1991, 1, 1) }
            };
            var db = MakeDb(snsSeed: seed);
            var sut = new SnsPacientesController(db.Object);

            var res = await sut.GetAll(CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(res);
            var list = Assert.IsAssignableFrom<List<SnsPacienteResponseDto>>(ok.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetById_Existente_200()
        {
            var seed = new[]
            {
                new SnsPaciente{ Id=10, NUtente="Z", NomeCompleto="ZZ", Nif="1", Telemovel="9", Morada="M", Email="z@z", DataNascimento=new DateTime(1992,1,1)}
            };
            var db = MakeDb(snsSeed: seed);

            // `FindAsync` precisa de setup explícito porque o controller usa FindAsync
            db.Setup(x => x.SnsPacientes.FindAsync(new object[] { 10 }, It.IsAny<CancellationToken>()))
              .ReturnsAsync(seed.First());

            var sut = new SnsPacientesController(db.Object);

            var res = await sut.GetById(10, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(res);
            var dto = Assert.IsType<SnsPacienteResponseDto>(ok.Value);
            Assert.Equal(10, dto.Id);
        }

        [Fact]
        public async Task GetById_Inexistente_404()
        {
            var db = MakeDb();
            db.Setup(x => x.SnsPacientes.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((SnsPaciente?)null);

            var sut = new SnsPacientesController(db.Object);

            var res = await sut.GetById(999, CancellationToken.None);
            Assert.IsType<NotFoundResult>(res);
        }

        [Fact]
        public async Task GetByNUtente_Existente_200()
        {
            var seed = new[]
            {
                new SnsPaciente { Id = 1, NUtente = "ABC", NomeCompleto = "Nome", Nif = "1", Telemovel = "9", Morada = "M", Email = "x@x", DataNascimento = new DateTime(2000, 1, 1) }
            };
            var db = MakeDb(snsSeed: seed);
            var sut = new SnsPacientesController(db.Object);

            var res = await sut.GetByNUtente("ABC", CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(res);
            var dto = Assert.IsType<SnsPacienteResponseDto>(ok.Value);
            Assert.Equal("ABC", dto.NUtente);
        }

        [Fact]
        public async Task GetByNUtente_Inexistente_404()
        {
            var db = MakeDb();
            var sut = new SnsPacientesController(db.Object);

            var res = await sut.GetByNUtente("NAO", CancellationToken.None);
            Assert.IsType<NotFoundResult>(res);
        }

        // =============================== UPDATE ===============================

        [Fact]
        public async Task Update_Valido_204_AtualizaCampos()
        {
            var ent = new SnsPaciente
            {
                Id = 5,
                NUtente = "U1",
                NomeCompleto = "Old",
                Nif = "1",
                Telemovel = "9",
                Morada = "M",
                Email = "o@o",
                DataNascimento = new DateTime(1990, 1, 1)
            };

            var db = MakeDb(snsSeed: new[] { ent });
            db.Setup(x => x.SnsPacientes.FindAsync(new object[] { 5 }, It.IsAny<CancellationToken>()))
              .ReturnsAsync(ent);

            var sut = new SnsPacientesController(db.Object);

            var dto = new UpdateSnsPacienteDto
            {
                NomeCompleto = " Maria ",
                Nif = " 987 ",
                Telemovel = " 911111111 ",
                Morada = " Av Y ",
                Email = " m@x.com ",
                DataNascimento = new DateTime(1985, 5, 5)
            };

            var res = await sut.Update(5, dto, CancellationToken.None);
            Assert.IsType<NoContentResult>(res);

            Assert.Equal("Maria", ent.NomeCompleto);
            Assert.Equal("987", ent.Nif);
            Assert.Equal("911111111", ent.Telemovel);
            Assert.Equal("Av Y", ent.Morada);
            Assert.Equal("m@x.com", ent.Email);
            Assert.Equal(new DateTime(1985, 5, 5), ent.DataNascimento);
        }

        [Theory]
        [InlineData(null!, "a", "a", "a", "a")]
        [InlineData("a", null!, "a", "a", "a")]
        [InlineData("a", "a", null!, "a", "a")]
        [InlineData("a", "a", "a", null!, "a")]
        [InlineData("a", "a", "a", "a", null!)]
        [InlineData("  ", "a", "a", "a", "a")]
        public async Task Update_CamposInvalidos_400(string nome, string nif, string tel, string morada, string email)
        {
            var db = MakeDb();
            var sut = new SnsPacientesController(db.Object);

            var dto = new UpdateSnsPacienteDto
            {
                NomeCompleto = nome,
                Nif = nif,
                Telemovel = tel,
                Morada = morada,
                Email = email,
                DataNascimento = new DateTime(2000, 1, 1)
            };

            var res = await sut.Update(1, dto, CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var msg = bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!.ToString();
            Assert.Contains("obrigatórios", msg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_Inexistente_404()
        {
            var db = MakeDb();
            db.Setup(x => x.SnsPacientes.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((SnsPaciente?)null);

            var sut = new SnsPacientesController(db.Object);

            var res = await sut.Update(999, new UpdateSnsPacienteDto
            {
                NomeCompleto = "A",
                Nif = "1",
                Telemovel = "9",
                Morada = "M",
                Email = "a@a",
                DataNascimento = new DateTime(2000, 1, 1)
            }, CancellationToken.None);

            Assert.IsType<NotFoundResult>(res);
        }

        // =============================== DELETE ===============================

        [Fact]
        public async Task Delete_Existente_204_E_Depois_404()
        {
            var ent = new SnsPaciente { Id = 2, NUtente = "U", NomeCompleto = "N", Nif = "1", Telemovel = "9", Morada = "M", Email = "x@x", DataNascimento = new DateTime(1990, 1, 1) };
            var db = MakeDb(snsSeed: new[] { ent });
            db.Setup(x => x.SnsPacientes.FindAsync(new object[] { 2 }, It.IsAny<CancellationToken>()))
              .ReturnsAsync(ent);

            // rastrear removidos
            var removed = new List<SnsPaciente>();
            db.Setup(x => x.SnsPacientes.Remove(It.IsAny<SnsPaciente>()))
              .Callback((SnsPaciente e) => removed.Add(e));

            var sut = new SnsPacientesController(db.Object);

            var no = await sut.Delete(2, CancellationToken.None);
            Assert.IsType<NoContentResult>(no);
            Assert.Single(removed);

            db.Setup(x => x.SnsPacientes.FindAsync(new object[] { 2 }, It.IsAny<CancellationToken>()))
              .ReturnsAsync((SnsPaciente?)null);

            var nf = await sut.Delete(2, CancellationToken.None);
            Assert.IsType<NotFoundResult>(nf);
        }

        [Fact]
        public async Task Delete_Inexistente_404()
        {
            var db = MakeDb();
            db.Setup(x => x.SnsPacientes.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((SnsPaciente?)null);

            var sut = new SnsPacientesController(db.Object);

            var res = await sut.Delete(999, CancellationToken.None);
            Assert.IsType<NotFoundResult>(res);
        }

        // =============================== IMPORTAR ===============================

        [Fact]
        public async Task Importar_BadRequest_QuandoNUtenteVazio()
        {
            var db = MakeDb();
            var sut = new SnsPacientesController(db.Object);

            var res = await sut.ImportarParaPaciente("   ", CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var msg = bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!.ToString();
            Assert.Contains("obrigatório", msg);
        }

        [Fact]
        public async Task Importar_NotFound_QuandoSnsInexistente()
        {
            var db = MakeDb();
            var sut = new SnsPacientesController(db.Object);

            var res = await sut.ImportarParaPaciente("U1", CancellationToken.None);
            var nf = Assert.IsType<NotFoundObjectResult>(res);
            var msg = nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!.ToString();
            Assert.Contains("Registo SNS", msg);
        }

        [Fact]
        public async Task Importar_NotFound_QuandoPacienteInexistente()
        {
            var sns = new[]
            {
                new SnsPaciente
                {
                    Id = 1, NUtente = "U1", NomeCompleto = "Novo",
                    Nif = "999", Telemovel = "933", Morada = "Nova",
                    Email = "novo@x", DataNascimento = new DateTime(1988, 8, 8)
                }
            };
            var db = MakeDb(snsSeed: sns); // sem paciente
            var sut = new SnsPacientesController(db.Object);

            var res = await sut.ImportarParaPaciente("U1", CancellationToken.None);
            var nf = Assert.IsType<NotFoundObjectResult>(res);
            var msg = nf.Value!.GetType().GetProperty("message")!.GetValue(nf.Value)!.ToString();
            Assert.Contains("Paciente", msg);
        }

        [Fact]
        public async Task Importar_Sucesso_AtualizaPacienteEDevolveOk()
        {
            var sns = new SnsPaciente
            {
                Id = 1,
                NUtente = "U1",
                NomeCompleto = "Novo Nome",
                Nif = "999",
                Telemovel = "933333333",
                Morada = "Nova",
                Email = "novo@x.com",
                DataNascimento = new DateTime(1988, 8, 8)
            };
            var paciente = new Paciente
            {
                Id = 10,
                NUtente = "U1",
                NomeCompleto = "Antigo",
                Nif = "111",
                Telemovel = "900000000",
                Morada = "Antiga",
                Email = "antigo@x.com",
                PasswordHash = "x",
                DataNascimento = new DateTime(1977, 7, 7)
            };

            var db = MakeDb(
                snsSeed: new[] { sns },
                pacSeed: new[] { paciente });

            var sut = new SnsPacientesController(db.Object);

            var res = await sut.ImportarParaPaciente("U1", CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(res);

            Assert.Equal("Novo Nome", paciente.NomeCompleto);
            Assert.Equal("999", paciente.Nif);
            Assert.Equal("933333333", paciente.Telemovel);
            Assert.Equal("Nova", paciente.Morada);
            Assert.Equal("novo@x.com", paciente.Email);
            Assert.Equal(new DateTime(1988, 8, 8), paciente.DataNascimento);

            var obj = ok.Value!;
            var msgProp = obj.GetType().GetProperty("message")!;
            Assert.Contains("sucesso", msgProp.GetValue(obj)!.ToString()!, StringComparison.OrdinalIgnoreCase);
        }
    }
}