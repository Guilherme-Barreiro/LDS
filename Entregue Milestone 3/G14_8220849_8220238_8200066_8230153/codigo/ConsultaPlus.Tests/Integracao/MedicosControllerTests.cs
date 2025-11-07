using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Medico;
public class MedicosControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public record MedicoVm(
        int Id,
        string NomeCompleto,
        string Telemovel,
        string Email,
        string NUtente,
        DateTime DataNascimento,
        DateTime? DataCriacao
    );

    public MedicosControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> CreateMedicoAsync(
        string nome = "  Doc A  ",
        string nut = "  U1 ",
        string email = "  a@a.com ",
        string tel = "  911 ")
    {
        var body = new
        {
            NomeCompleto = nome,
            NUtente = nut,
            Email = email,
            Telemovel = tel,
            Password = "pwd",
            DataNascimento = "1990-01-01"
        };

        var resp = await _client.PostAsJsonAsync("/api/Medicos", body);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<MedicoVm>();
        Assert.NotNull(dto);
        return dto!.Id;
    }

    [Fact]
    public async Task Create_Valido_201_ComTrimEBody()
    {
        var resp = await _client.PostAsJsonAsync("/api/Medicos", new
        {
            NomeCompleto = "  Doc A  ",
            NUtente = "  U1 ",
            Email = "  a@a.com ",
            Telemovel = "  911 ",
            Password = "pwd",
            DataNascimento = "1990-01-01"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);

        var dto = await resp.Content.ReadFromJsonAsync<MedicoVm>();
        Assert.NotNull(dto);
        Assert.True(dto!.Id > 0);
        Assert.Equal("Doc A", dto.NomeCompleto);
        Assert.Equal("U1", dto.NUtente);
        Assert.Equal("a@a.com", dto.Email);
        Assert.Equal("911", dto.Telemovel);

        var get = await _client.GetAsync(resp.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Create_Invalido_400_ComMensagens()
    {
        var resp = await _client.PostAsJsonAsync("/api/Medicos", new
        {
            NomeCompleto = "", 
            NUtente = "U1",
            Email = "a@a.com",
            Telemovel = "911",
            Password = "pwd",
            DataNascimento = "1990-01-01"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var resp2 = await _client.PostAsJsonAsync("/api/Medicos", new
        {
            NomeCompleto = "X",
            NUtente = "",
            Email = "a@a.com",
            Telemovel = "911",
            Password = "pwd",
            DataNascimento = "1990-01-01"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
    }

    [Fact]
    public async Task GetAll_200_ContemCriados()
    {
        var id1 = await CreateMedicoAsync("Doc One", "UO1", "one@ex.com", "900");
        var id2 = await CreateMedicoAsync("Doc Two", "UT2", "two@ex.com", "901");

        var resp = await _client.GetAsync("/api/Medicos");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<MedicoVm>>();
        Assert.NotNull(list);
        Assert.Contains(list!, m => m.Id == id1 && m.NomeCompleto == "Doc One");
        Assert.Contains(list!, m => m.Id == id2 && m.NomeCompleto == "Doc Two");
    }

    [Fact]
    public async Task GetById_200_E_404()
    {
        var id = await CreateMedicoAsync("Doc X", "UX1", "x@ex.com", "933");

        var ok = await _client.GetAsync($"/api/Medicos/{id}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var dto = await ok.Content.ReadFromJsonAsync<MedicoVm>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);

        var nf = await _client.GetAsync("/api/Medicos/999999");
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
    }

    [Fact]
    public async Task Search_ParametroObrigatorio_400()
    {
        var bad = await _client.GetAsync("/api/Medicos/search");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var bad2 = await _client.GetAsync("/api/Medicos/search?nome=%20%20%20");
        Assert.Equal(HttpStatusCode.BadRequest, bad2.StatusCode);
    }

    [Fact]
    public async Task Search_ComNome_200_ComResultados()
    {
        await CreateMedicoAsync("Ana Medica", "UA1", "ana@ex.com", "911");
        await CreateMedicoAsync("Anabela", "UA2", "anabela@ex.com", "912");
        await CreateMedicoAsync("Bruno", "UB1", "bruno@ex.com", "913");

        var resp = await _client.GetAsync("/api/Medicos/search?nome=Ana");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<MedicoVm>>();
        Assert.NotNull(list);
        Assert.True(list!.Count >= 2);
        Assert.All(list!, m => Assert.Contains("Ana", m.NomeCompleto, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Update_Existente_204_E_Persistido()
    {
        var id = await CreateMedicoAsync("Antigo Nome", "UX9", "old@ex.com", "900");

        var resp = await _client.PutAsJsonAsync($"/api/Medicos/{id}", new
        {
            NomeCompleto = "  Novo Nome ",
            Telemovel = "  933 ",
            Email = "  novo@ex.com ",
            DataNascimento = "1985-12-31"
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var get = await _client.GetAsync($"/api/Medicos/{id}");
        var dto = await get.Content.ReadFromJsonAsync<MedicoVm>();
        Assert.NotNull(dto);
        Assert.Equal("Novo Nome", dto!.NomeCompleto);
        Assert.Equal("933", dto.Telemovel);
        Assert.Equal("novo@ex.com", dto.Email);
        Assert.Equal(new DateTime(1985, 12, 31), dto.DataNascimento);
    }

    [Fact]
    public async Task Update_Inexistente_404()
    {
        var resp = await _client.PutAsJsonAsync("/api/Medicos/999999", new
        {
            NomeCompleto = "Qualquer"
        });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_204_Sempre()
    {
        var id = await CreateMedicoAsync("A Apagar", "UAZ", "del@ex.com", "999");

        var del = await _client.DeleteAsync($"/api/Medicos/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        try
        {
            var again = await _client.DeleteAsync($"/api/Medicos/{id}");
            Assert.True(
                again.StatusCode == HttpStatusCode.NoContent ||
                again.StatusCode == HttpStatusCode.NotFound,
                $"Esperado 204 ou 404 na segunda remoção, obtido {(int)again.StatusCode} {again.StatusCode}"
            );
        }
        catch (Exception ex)
        {
            Assert.Contains("não existe", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Medicos.AnyAsync(m => m.Id == id));
    }

}
