using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Especialidade;

public class EspecialidadeControllerE2ETests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public record EspVm(int Id, string Nome);

    public EspecialidadeControllerE2ETests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> CreateEspecialidadeAsync(string nome)
    {
        var resp = await _client.PostAsJsonAsync("/api/Especialidade/registo-especialidade", new { nome });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<EspVm>();
        Assert.NotNull(dto);
        return dto!.Id;
    }

    // ---------- POST registo-especialidade -> 201 + body
    [Fact]
    public async Task Create_Valido_201_ComBody()
    {
        var resp = await _client.PostAsJsonAsync("/api/Especialidade/registo-especialidade", new { nome = "Cardiologia" });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<EspVm>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("Cardiologia", body.Nome);
    }

    // ---------- POST duplicado -> 409 (InvalidOperationException do service)
    [Fact]
    public async Task Create_Duplicado_409()
    {
        await CreateEspecialidadeAsync("Oncologia");
        var resp2 = await _client.PostAsJsonAsync("/api/Especialidade/registo-especialidade", new { nome = "Oncologia" });
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    // ---------- POST invalido -> 400 (ArgumentException/Exception no service)
    [Fact]
    public async Task Create_NomeInvalido_400()
    {
        var resp = await _client.PostAsJsonAsync("/api/Especialidade/registo-especialidade", new { nome = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---------- GET por id existente -> 200
    [Fact]
    public async Task GetById_Existente_200()
    {
        var id = await CreateEspecialidadeAsync("Dermatologia");

        var resp = await _client.GetAsync($"/api/Especialidade/obter-especialidade-id/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<EspVm>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);
        Assert.Equal("Dermatologia", dto.Nome);
    }

    // ---------- GET por id inexistente -> 404
    [Fact]
    public async Task GetById_Inexistente_404()
    {
        var resp = await _client.GetAsync("/api/Especialidade/obter-especialidade-id/999999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---------- GET por nome (rota tem token {string}) -> 200 com resultados
    [Fact]
    public async Task GetByNome_ComResultados_200()
    {
        await CreateEspecialidadeAsync("Neurocirurgia");
        await CreateEspecialidadeAsync("Neurologia");

        // Atenção: o template está como {string}; usamos um segmento qualquer "Neuro"
        var resp = await _client.GetAsync("/api/Especialidade/obter-especialidade-nome/Neuro");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<EspVm>>();
        Assert.NotNull(list);
        Assert.True(list!.Count >= 1);
        Assert.All(list!, e => Assert.Contains("Neuro", e.Nome, StringComparison.OrdinalIgnoreCase));
    }

    // ---------- GET por nome sem resultados -> 200 lista vazia (controller só 404 se service devolver null)
    [Fact]
    public async Task GetByNome_SemResultados_200_Vazio()
    {
        var resp = await _client.GetAsync("/api/Especialidade/obter-especialidade-nome/ZZZInexistente");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<EspVm>>();
        Assert.NotNull(list);
        Assert.Empty(list!);
    }

    // ---------- GET todas -> 200 com lista
    [Fact]
    public async Task GetAll_200_ComLista()
    {
        await CreateEspecialidadeAsync("Ortopedia");
        await CreateEspecialidadeAsync("Pediatria");

        var resp = await _client.GetAsync("/api/Especialidade/obter-todas-especialidades");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<EspVm>>();
        Assert.NotNull(list);
        Assert.True(list!.Count >= 2);
        Assert.Contains(list, e => e.Nome == "Ortopedia");
        Assert.Contains(list, e => e.Nome == "Pediatria");
    }

    // ---------- PUT atualizar-especialidade/{id} -> 204 sucesso
    [Fact]
    public async Task Update_Sucesso_204()
    {
        var id = await CreateEspecialidadeAsync("Gastrenterologia");

        var resp = await _client.PutAsJsonAsync($"/api/Especialidade/atualizar-especialidade/{id}",
            new { nome = "Gastroenterologia" });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/Especialidade/obter-especialidade-id/{id}");
        var dto = await get.Content.ReadFromJsonAsync<EspVm>();
        Assert.NotNull(dto);
        Assert.Equal("Gastroenterologia", dto!.Nome);
    }

    // ---------- PUT nome inválido -> 400 (InvalidOperationException -> BadRequest no controller)
    [Fact]
    public async Task Update_NomeInvalido_400()
    {
        var id = await CreateEspecialidadeAsync("Reumatologia");

        var resp = await _client.PutAsJsonAsync($"/api/Especialidade/atualizar-especialidade/{id}",
            new { nome = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---------- PUT duplicado -> 400 (controller mapeia InvalidOperationException para BadRequest)
    [Fact]
    public async Task Update_Duplicado_400()
    {
        var id1 = await CreateEspecialidadeAsync("Hematologia");
        var id2 = await CreateEspecialidadeAsync("Urologia");

        var resp = await _client.PutAsJsonAsync($"/api/Especialidade/atualizar-especialidade/{id2}",
            new { nome = "Hematologia" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---------- DELETE remover-especialidade/{id} -> 204; se não existir, controller devolve 400 (catch genérico)
    [Fact]
    public async Task Delete_204_E_Inexistente_400()
    {
        var id = await CreateEspecialidadeAsync("Otorrinolaringologia");

        var del = await _client.DeleteAsync($"/api/Especialidade/remover-especialidade/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var again = await _client.DeleteAsync($"/api/Especialidade/remover-especialidade/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Especialidades.AnyAsync(s => s.Id == id));
    }
}
