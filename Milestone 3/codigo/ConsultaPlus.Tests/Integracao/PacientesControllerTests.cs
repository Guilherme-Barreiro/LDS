using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Pacientes
{
    public class PacientesControllerIT : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public PacientesControllerIT(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(); 
        }

        public record PacienteVm(
            int Id,
            string? NomeCompleto,
            string? Nif,
            string NUtente,
            string? Telemovel,
            string? Morada,
            string? Email,
            DateTime? DataNascimento,
            DateTime? DataCriacao
        );

        private async Task CleanupAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Pacientes.RemoveRange(db.Pacientes);
            await db.SaveChangesAsync();
        }

        private async Task<int> CreatePacienteAsync(
            string nut = "  U1 ",
            string password = "pwd",
            string? nome = "  Ana  ",
            string? email = "  a@a.com ",
            string? tel = "  911 ",
            string? morada = "  Rua ",
            string? nif = "  123 "
        )
        {
            var body = new
            {
                NUtente = nut,
                Password = password,
                NomeCompleto = nome,
                Email = email,
                Telemovel = tel,
                Morada = morada,
                Nif = nif,
                DataNascimento = "1990-01-01"
            };

            var resp = await _client.PostAsJsonAsync("/api/Pacientes", body);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var dto = await resp.Content.ReadFromJsonAsync<PacienteVm>();
            Assert.NotNull(dto);
            return dto!.Id;
        }


        [Fact]
        public async Task Create_201_ComTrim_E_Location()
        {
            await CleanupAsync();

            var resp = await _client.PostAsJsonAsync("/api/Pacientes", new
            {
                NUtente = "  U-ABC ",
                Password = "pwd",
                NomeCompleto = "  Ana  ",
                Email = "  a@a.com ",
                Telemovel = "  911 ",
                Morada = "  Rua ",
                Nif = "  123 ",
                DataNascimento = "1990-01-01"
            });

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            Assert.NotNull(resp.Headers.Location);

            var dto = await resp.Content.ReadFromJsonAsync<PacienteVm>();
            Assert.NotNull(dto);
            Assert.True(dto!.Id > 0);
            Assert.Equal("U-ABC", dto.NUtente);
            Assert.Equal("Ana", dto.NomeCompleto);
            Assert.Equal("a@a.com", dto.Email);
            Assert.Equal("911", dto.Telemovel);
            Assert.Equal("Rua", dto.Morada);
            Assert.Equal("123", dto.Nif);

            var get = await _client.GetAsync(resp.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }

        [Fact]
        public async Task Create_400_QuandoObrigatoriosEmFalta()
        {
            await CleanupAsync();

            var r1 = await _client.PostAsJsonAsync("/api/Pacientes", new { NUtente = "", Password = "pwd" });
            var r2 = await _client.PostAsJsonAsync("/api/Pacientes", new { NUtente = "U1", Password = "" });

            Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        }

        [Fact]
        public async Task Create_409_QuandoNUtenteDuplicado()
        {
            await CleanupAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Pacientes.Add(new ConsultaPlus.Core.Models.Paciente
                {
                    NUtente = "DUP",
                    PasswordHash = "x" 
                });
                await db.SaveChangesAsync();
            }

            var r = await _client.PostAsJsonAsync("/api/Pacientes", new { NUtente = " DUP ", Password = "pwd" });
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        }

        [Fact]
        public async Task GetAll_200_ContemCriados()
        {
            await CleanupAsync();
            var id1 = await CreatePacienteAsync("UA1", "pwd", "Ana");
            var id2 = await CreatePacienteAsync("UB2", "pwd", "Bruno");

            var resp = await _client.GetAsync("/api/Pacientes");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<PacienteVm>>();
            Assert.NotNull(list);
            Assert.Contains(list!, p => p.Id == id1 && p.NomeCompleto == "Ana");
            Assert.Contains(list!, p => p.Id == id2 && p.NomeCompleto == "Bruno");
        }

        [Fact]
        public async Task GetById_200_E_404()
        {
            await CleanupAsync();
            var id = await CreatePacienteAsync("UX1");

            var ok = await _client.GetAsync($"/api/Pacientes/{id}");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

            var nf = await _client.GetAsync("/api/Pacientes/999999");
            Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
        }

        [Fact]
        public async Task Update_204_E_Persistido()
        {
            await CleanupAsync();
            var id = await CreatePacienteAsync("UZ9", "pwd", "Antigo");

            var put = await _client.PutAsJsonAsync($"/api/Pacientes/{id}", new
            {
                NomeCompleto = "Novo Nome",
                Email = "novo@ex.com",
                Telemovel = "933"
            });
            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

            var get = await _client.GetAsync($"/api/Pacientes/{id}");
            var dto = await get.Content.ReadFromJsonAsync<PacienteVm>();
            Assert.NotNull(dto);
            Assert.Equal("Novo Nome", dto!.NomeCompleto);
            Assert.Equal("novo@ex.com", dto.Email);
            Assert.Equal("933", dto.Telemovel);
        }

        [Fact]
        public async Task Update_404_QuandoNaoExiste()
        {
            await CleanupAsync();

            var r = await _client.PutAsJsonAsync("/api/Pacientes/999999", new { NomeCompleto = "Qualquer" });
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        }

        [Fact]
        public async Task Delete_204_E_AposIsso_GetById_404()
        {
            await CleanupAsync();
            var id = await CreatePacienteAsync("UDEL");

            var del = await _client.DeleteAsync($"/api/Pacientes/{id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var nf = await _client.GetAsync($"/api/Pacientes/{id}");
            Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
        }
    }
}
