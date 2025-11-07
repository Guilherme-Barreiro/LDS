using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Sala;

public class SalasControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public record SalaVm(int Id, string Nome);

    public SalasControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> GetSalaIdByNameAsync(string nome)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = await db.Salas.Where(s => s.Nome == nome).Select(s => s.Id).FirstOrDefaultAsync();
        return id;
    }

    private async Task<int> CreateSalaAsync(string nome)
    {
        var resp = await _client.PostAsJsonAsync("/api/Salas", new { nome });
        if (resp.StatusCode == HttpStatusCode.Created)
        {
            var dto = await resp.Content.ReadFromJsonAsync<SalaVm>();
            return dto!.Id;
        }
        throw new Exception($"Falha a criar sala '{nome}': {resp.StatusCode}");
    }

    [Fact]
    public async Task Create_Valido_201_ComBodyELocation()
    {
        var resp = await _client.PostAsJsonAsync("/api/Salas", new { nome = "Sala Azul" });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);

        var body = await resp.Content.ReadFromJsonAsync<SalaVm>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("Sala Azul", body.Nome);

        var get = await _client.GetAsync(resp.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task GetById_Existente_200()
    {
        var id = await CreateSalaAsync("Sala Verde");

        var resp = await _client.GetAsync($"/api/Salas/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<SalaVm>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);
        Assert.Equal("Sala Verde", dto.Nome);
    }

    [Fact]
    public async Task GetById_Inexistente_404()
    {
        var resp = await _client.GetAsync("/api/Salas/999999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetAll_200_ComLista()
    {
        await CreateSalaAsync("Sala A");
        await CreateSalaAsync("Sala B");

        var resp = await _client.GetAsync("/api/Salas");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<SalaVm>>();
        Assert.NotNull(list);
        Assert.True(list!.Count >= 2);
        Assert.Contains(list, s => s.Nome == "Sala A");
        Assert.Contains(list, s => s.Nome == "Sala B");
    }

    [Fact]
    public async Task Search_SemNome_400()
    {
        var resp = await _client.GetAsync("/api/Salas/search");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Search_ComNome_200_ComResultados()
    {
        await CreateSalaAsync("Laboratório 1");
        await CreateSalaAsync("Laboratório 2");
        await CreateSalaAsync("Sala Comum");

        var resp = await _client.GetAsync("/api/Salas/search?nome=Laboratório");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<SalaVm>>();
        Assert.NotNull(list);
        Assert.True(list!.Count >= 2);
        Assert.All(list!, s => Assert.Contains("Laboratório", s.Nome));
    }

    [Fact]
    public async Task Create_Duplicado_409()
    {
        await CreateSalaAsync("Sala Repetida");

        var resp2 = await _client.PostAsJsonAsync("/api/Salas", new { nome = "Sala Repetida" });
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task Create_NomeInvalido_400()
    {
        var resp = await _client.PostAsJsonAsync("/api/Salas", new { nome = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_204_E_Depois_404()
    {
        var id = await CreateSalaAsync("Sala Temp");

        var del = await _client.DeleteAsync($"/api/Salas/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var again = await _client.DeleteAsync($"/api/Salas/{id}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Salas.AnyAsync(s => s.Id == id));
    }
}
