using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Tests.Integracao.Especialidade
{
    public class EspecialidadeControllerTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public record EspVm(int Id, string Nome);

        public EspecialidadeControllerTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<int> CreateEspecialidadeAsync(string nome)
        {
            var resp = await _client.PostAsJsonAsync("/api/Especialidade", new { nome });
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var dto = await resp.Content.ReadFromJsonAsync<EspVm>();
            Assert.NotNull(dto);
            return dto!.Id;
        }

        [Fact]
        public async Task Create_Valido_201_ComBody()
        {
            var resp = await _client.PostAsJsonAsync("/api/Especialidade", new { Nome = "Cardiologia" });
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            var body = await resp.Content.ReadFromJsonAsync<EspVm>();
            Assert.NotNull(body);
            Assert.True(body!.Id > 0);
            Assert.Equal("Cardiologia", body.Nome);
        }

        [Fact]
        public async Task Create_Duplicado_409()
        {
            await CreateEspecialidadeAsync("Oncologia");
            var resp2 = await _client.PostAsJsonAsync("/api/Especialidade", new { Nome = "Oncologia" });
            Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
        }

        [Fact]
        public async Task Create_NomeInvalido_400_ou_409()
        {
            var resp = await _client.PostAsJsonAsync("/api/Especialidade", new { Nome = "   " });
            Assert.Contains(resp.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict });
        }

        [Fact]
        public async Task GetById_Existente_200()
        {
            var id = await CreateEspecialidadeAsync("Dermatologia");

            var resp = await _client.GetAsync($"/api/Especialidade/{id}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var dto = await resp.Content.ReadFromJsonAsync<EspVm>();
            Assert.NotNull(dto);
            Assert.Equal(id, dto!.Id);
            Assert.Equal("Dermatologia", dto.Nome);
        }

        [Fact]
        public async Task GetById_Inexistente_404()
        {
            var resp = await _client.GetAsync("/api/Especialidade/999999");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Search_ComResultados_200()
        {
            await CreateEspecialidadeAsync("Neurocirurgia");
            await CreateEspecialidadeAsync("Neurologia");

            var resp = await _client.GetAsync("/api/Especialidade/search?nome=Neuro");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<EspVm>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
            Assert.All(list!, e => Assert.Contains("Neuro", e.Nome, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Search_SemResultados_404()
        {
            var resp = await _client.GetAsync("/api/Especialidade/search?nome=ZZZInexistente");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task GetAll_200_ComLista()
        {
            await CreateEspecialidadeAsync("Ortopedia");
            await CreateEspecialidadeAsync("Pediatria");

            var resp = await _client.GetAsync("/api/Especialidade");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<EspVm>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 2);
            Assert.Contains(list, e => e.Nome == "Ortopedia");
            Assert.Contains(list, e => e.Nome == "Pediatria");
        }

        [Fact]
        public async Task Update_Sucesso_204()
        {
            var id = await CreateEspecialidadeAsync("Gastrenterologia");

            var resp = await _client.PutAsJsonAsync($"/api/Especialidade/{id}",
                new { nome = "Gastroenterologia" });
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

            var get = await _client.GetAsync($"/api/Especialidade/{id}");
            var dto = await get.Content.ReadFromJsonAsync<EspVm>();
            Assert.NotNull(dto);
            Assert.Equal("Gastroenterologia", dto!.Nome);
        }

        [Fact]
        public async Task Update_NomeInvalido_400_ou_409()
        {
            var id = await CreateEspecialidadeAsync("Reumatologia");

            var resp = await _client.PutAsJsonAsync($"/api/Especialidade/{id}",
                new { nome = "   " });
            Assert.Contains(resp.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict });
        }

        [Fact]
        public async Task Update_Duplicado_409()
        {
            var id1 = await CreateEspecialidadeAsync("Hematologia");
            var id2 = await CreateEspecialidadeAsync("Urologia");

            var resp = await _client.PutAsJsonAsync($"/api/Especialidade/{id2}",
                new { nome = "Hematologia" });
            Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_204_E_Inexistente_404()
        {
            var id = await CreateEspecialidadeAsync("Otorrinolaringologia");

            var del = await _client.DeleteAsync($"/api/Especialidade/{id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var again = await _client.DeleteAsync($"/api/Especialidade/{id}");
            Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.Especialidades.AnyAsync(s => s.Id == id));
        }
    }
}
