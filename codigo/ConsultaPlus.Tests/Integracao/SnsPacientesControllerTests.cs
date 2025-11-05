using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.SNS
{
    public class SnsPacientesControllerTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private const string BaseUrl = "/api/SnsPacientes";

        public SnsPacientesControllerTests(ApiFactory factory) => _factory = factory;

        // DTOs p/ (de)serialização
        public record CreateDto(string NUtente, string NomeCompleto, string Nif, string Telemovel, string Morada, string Email, DateTime DataNascimento);
        public record UpdateDto(string NomeCompleto, string Nif, string Telemovel, string Morada, string Email, DateTime DataNascimento);
        public record RespDto(int Id, string NUtente, string NomeCompleto, string Nif, string Telemovel, string Morada, string Email, DateTime DataNascimento, DateTime DataCriacao);

        private static CreateDto NewCreate(
            string nUtente = "UT-ABC-001",
            string nome = "Ana Silva",
            string nif = "123456789",
            string tel = "910000000",
            string morada = "Rua A",
            string email = "ana@ex.com",
            DateTime? nasc = null
        ) => new(nUtente, nome, nif, tel, morada, email, nasc ?? new DateTime(1990, 1, 2));

        private static UpdateDto NewUpdate(
            string nome = "Ana Atualizada",
            string nif = "987654321",
            string tel = "930000000",
            string morada = "Rua B",
            string email = "ana.atual@ex.com",
            DateTime? nasc = null
        ) => new(nome, nif, tel, morada, email, nasc ?? new DateTime(1991, 5, 10));

        // Utilitário: cria cliente e devolve db limpa por teste
        private (HttpClient client, ApplicationDbContext db) CreateClientAndDb()
        {
            var client = _factory.CreateClient(); // já autenticado pelo TestAuthHandler
            var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Limpeza completa para isolamento
            db.SnsPacientes.RemoveRange(db.SnsPacientes);
            db.Pacientes.RemoveRange(db.Pacientes);
            db.SaveChanges();

            return (client, db);
        }

        // ============ TESTES ============

        [Fact]
        public async Task Create_201_PersisteEDevolveLocation()
        {
            var (client, db) = CreateClientAndDb();
            var dto = NewCreate("UT-X-001");

            var res = await client.PostAsJsonAsync(BaseUrl, dto);

            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<RespDto>();
            Assert.NotNull(body);
            Assert.Equal(dto.NUtente, body!.NUtente);

            var saved = await db.SnsPacientes.AsNoTracking().FirstOrDefaultAsync(x => x.NUtente == dto.NUtente);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task Create_400_QuandoCamposEmFalta()
        {
            var (client, _) = CreateClientAndDb();
            var dto = NewCreate(nome: " ", email: " ");

            var res = await client.PostAsJsonAsync(BaseUrl, dto);

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task Create_409_DuplicateNUtente()
        {
            var (client, db) = CreateClientAndDb();
            db.SnsPacientes.Add(new SnsPaciente
            {
                NUtente = "UT-DUP",
                NomeCompleto = "X",
                Nif = "111111111",
                Telemovel = "900000000",
                Morada = "R",
                Email = "x@x.com",
                DataNascimento = new DateTime(1990, 1, 1)
            });
            await db.SaveChangesAsync();

            var res = await client.PostAsJsonAsync(BaseUrl, NewCreate("UT-DUP"));

            Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        }

        [Fact]
        public async Task GetAll_200_DevolveLista()
        {
            var (client, db) = CreateClientAndDb();
            db.SnsPacientes.AddRange(
                new SnsPaciente { NUtente = "UT-1", NomeCompleto = "A", Nif = "111111111", Telemovel = "911111111", Morada = "R1", Email = "a@x.com", DataNascimento = new DateTime(1980, 1, 1) },
                new SnsPaciente { NUtente = "UT-2", NomeCompleto = "B", Nif = "222222222", Telemovel = "922222222", Morada = "R2", Email = "b@x.com", DataNascimento = new DateTime(1981, 1, 1) }
            );
            await db.SaveChangesAsync();

            var res = await client.GetAsync(BaseUrl);

            res.EnsureSuccessStatusCode();
            var list = await res.Content.ReadFromJsonAsync<List<RespDto>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 2);
        }

        [Fact]
        public async Task GetById_200_Encontrado()
        {
            var (client, db) = CreateClientAndDb();
            var e = new SnsPaciente { NUtente = "UT-ID-OK", NomeCompleto = "C", Nif = "333333333", Telemovel = "933333333", Morada = "R3", Email = "c@x.com", DataNascimento = new DateTime(1982, 1, 1) };
            db.SnsPacientes.Add(e);
            await db.SaveChangesAsync();

            var res = await client.GetAsync($"{BaseUrl}/{e.Id}");

            res.EnsureSuccessStatusCode();
            var dto = await res.Content.ReadFromJsonAsync<RespDto>();
            Assert.Equal(e.NUtente, dto!.NUtente);
        }

        [Fact]
        public async Task GetById_404_QuandoNaoExiste()
        {
            var (client, _) = CreateClientAndDb();

            var res = await client.GetAsync($"{BaseUrl}/99999");

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task GetByNUtente_200_Encontrado()
        {
            var (client, db) = CreateClientAndDb();
            var e = new SnsPaciente { NUtente = "UT-ABC-NU", NomeCompleto = "D", Nif = "444444444", Telemovel = "944444444", Morada = "R4", Email = "d@x.com", DataNascimento = new DateTime(1983, 1, 1) };
            db.SnsPacientes.Add(e);
            await db.SaveChangesAsync();

            // usar string não numérica para não coincidir com {id:int}
            var res = await client.GetAsync($"{BaseUrl}/{e.NUtente}");

            res.EnsureSuccessStatusCode();
            var dto = await res.Content.ReadFromJsonAsync<RespDto>();
            Assert.Equal(e.NUtente, dto!.NUtente);
        }

        [Fact]
        public async Task GetByNUtente_404_QuandoNaoExiste()
        {
            var (client, _) = CreateClientAndDb();

            var res = await client.GetAsync($"{BaseUrl}/UT-INEXISTENTE");

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Update_204_AtualizaCampos()
        {
            var (client, db) = CreateClientAndDb();
            var e = new SnsPaciente { NUtente = "UT-UPD", NomeCompleto = "E", Nif = "555555555", Telemovel = "955555555", Morada = "R5", Email = "e@x.com", DataNascimento = new DateTime(1984, 1, 1) };
            db.SnsPacientes.Add(e);
            await db.SaveChangesAsync();

            var res = await client.PutAsJsonAsync($"{BaseUrl}/{e.Id}", NewUpdate(nome: "E Atual"));
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

            var updated = await db.SnsPacientes.AsNoTracking().FirstAsync(x => x.Id == e.Id);
            Assert.Equal("E Atual", updated.NomeCompleto);
        }

        [Fact]
        public async Task Update_404_QuandoNaoExiste()
        {
            var (client, _) = CreateClientAndDb();

            var res = await client.PutAsJsonAsync($"{BaseUrl}/123456", NewUpdate());

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Update_400_QuandoCamposEmFalta()
        {
            var (client, db) = CreateClientAndDb();
            var e = new SnsPaciente { NUtente = "UT-UPD-ERR", NomeCompleto = "F", Nif = "666666666", Telemovel = "966666666", Morada = "R6", Email = "f@x.com", DataNascimento = new DateTime(1985, 1, 1) };
            db.SnsPacientes.Add(e);
            await db.SaveChangesAsync();

            var bad = new UpdateDto(" ", " ", " ", " ", " ", new DateTime(2000, 1, 1));

            var res = await client.PutAsJsonAsync($"{BaseUrl}/{e.Id}", bad);

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task Delete_204_Remove()
        {
            var (client, db) = CreateClientAndDb();
            var e = new SnsPaciente { NUtente = "UT-DEL", NomeCompleto = "G", Nif = "777777777", Telemovel = "977777777", Morada = "R7", Email = "g@x.com", DataNascimento = new DateTime(1986, 1, 1) };
            db.SnsPacientes.Add(e);
            await db.SaveChangesAsync();

            var res = await client.DeleteAsync($"{BaseUrl}/{e.Id}");
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

            var exists = await db.SnsPacientes.AsNoTracking().AnyAsync(x => x.Id == e.Id);
            Assert.False(exists);
        }

        [Fact]
        public async Task Delete_404_QuandoNaoExiste()
        {
            var (client, _) = CreateClientAndDb();

            var res = await client.DeleteAsync($"{BaseUrl}/99999");

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Importar_400_QuandoNUtenteVazio()
        {
            var (client, _) = CreateClientAndDb();

            var res = await client.PostAsync($"{BaseUrl}/importar/%20", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task Importar_404_QuandoSnsNaoExiste()
        {
            var (client, db) = CreateClientAndDb();
            // Existe paciente mas não existe SNS
            db.Pacientes.Add(new Paciente { NUtente = "UT-IMP-1", NomeCompleto = "P", Email = "p@x.com", PasswordHash = "test" });
            await db.SaveChangesAsync();

            var res = await client.PostAsync($"{BaseUrl}/importar/UT-IMP-1", content: null);

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Importar_404_QuandoPacienteNaoExiste()
        {
            var (client, db) = CreateClientAndDb();
            // Existe SNS mas não existe Paciente
            db.SnsPacientes.Add(new SnsPaciente
            {
                NUtente = "UT-IMP-2",
                NomeCompleto = "S",
                Nif = "888888888",
                Telemovel = "988888888",
                Morada = "R8",
                Email = "s@x.com",
                DataNascimento = new DateTime(1987, 1, 1)
            });
            await db.SaveChangesAsync();

            var res = await client.PostAsync($"{BaseUrl}/importar/UT-IMP-2", content: null);

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Importar_200_CopiaCamposParaPaciente()
        {
            var (client, db) = CreateClientAndDb();

            db.SnsPacientes.Add(new SnsPaciente
            {
                NUtente = "UT-IMP-OK",
                NomeCompleto = "Nome SNS",
                Nif = "999999999",
                Telemovel = "999999999",
                Morada = "Rua SNS",
                Email = "sns@x.com",
                DataNascimento = new DateTime(1999, 9, 9)
            });

            db.Pacientes.Add(new Paciente
            {
                NUtente = "UT-IMP-OK",
                NomeCompleto = "Nome Paciente",
                Email = "pac@x.com",
                PasswordHash = "test"
            });

            await db.SaveChangesAsync();

            var res = await client.PostAsync($"{BaseUrl}/importar/UT-IMP-OK", content: null);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var paciente = await db.Pacientes.AsNoTracking().FirstAsync(p => p.NUtente == "UT-IMP-OK");
            Assert.Equal("Nome SNS", paciente.NomeCompleto);
            Assert.Equal("999999999", paciente.Nif);
            Assert.Equal("999999999", paciente.Telemovel);
            Assert.Equal("Rua SNS", paciente.Morada);
            Assert.Equal("sns@x.com", paciente.Email);
            Assert.Equal(new DateTime(1999, 9, 9), paciente.DataNascimento);
        }
    }
}
