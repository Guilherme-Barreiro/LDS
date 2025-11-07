using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Notificacoes;
public class NotificacoesControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public record NotificacaoVm(
        int Id,
        string Categoria,
        string Descricao,
        DateTime DataCriacao,
        bool Lida,
        int? MedicoId,
        int? PacienteId
    );

    public NotificacoesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> CreateAsync(string categoria, string descricao, int? medicoId = null, int? pacienteId = null)
    {
        var resp = await _client.PostAsJsonAsync("/api/Notificacoes", new
        {
            categoria,
            descricao,
            medicoId,
            pacienteId
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<NotificacaoVm>();
        Assert.NotNull(dto);
        return dto!.Id;
    }

    [Fact]
    public async Task Create_Valida_201_ComBody()
    {
        var resp = await _client.PostAsJsonAsync("/api/Notificacoes", new
        {
            categoria = "  Sistema ",
            descricao = "  Atualização disponível  ",
            medicoId = 7,
            pacienteId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.NotNull(resp.Headers.Location);

        var body = await resp.Content.ReadFromJsonAsync<NotificacaoVm>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("Sistema", body.Categoria);
        Assert.Equal("Atualização disponível", body.Descricao);
        Assert.Equal(7, body.MedicoId);
        Assert.Null(body.PacienteId);

        var get = await _client.GetAsync(resp.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Create_Invalida_400()
    {
        var resp = await _client.PostAsJsonAsync("/api/Notificacoes", new
        {
            categoria = "   ",
            descricao = "x"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var resp2 = await _client.PostAsJsonAsync("/api/Notificacoes", new
        {
            categoria = "x",
            descricao = "   "
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
    }

    [Fact]
    public async Task GetAll_200_ComLista()
    {
        var id1 = await CreateAsync("Sistema", "A");
        var id2 = await CreateAsync("Alertas", "B", medicoId: 10);

        var resp = await _client.GetAsync("/api/Notificacoes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<NotificacaoVm>>();
        Assert.NotNull(list);
        Assert.Contains(list!, n => n.Id == id1);
        Assert.Contains(list!, n => n.Id == id2);
    }

    [Fact]
    public async Task Get_Filter_Medico_UnreadOnly_200()
    {
        var n1 = await CreateAsync("M1", "A", medicoId: 1);
        var n2 = await CreateAsync("M1", "B", medicoId: 1);
        var patch = await _client.PatchAsync($"/api/Notificacoes/{n1}/ler?Lida=true", null);
        Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);

        await CreateAsync("M2", "C", medicoId: 2);

        var resp = await _client.GetAsync("/api/Notificacoes?medicoId=1&unreadOnly=true");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<NotificacaoVm>>();
        Assert.NotNull(list);
        Assert.Single(list!);
        Assert.Equal(n2, list![0].Id);
        Assert.False(list[0].Lida);
        Assert.Equal(1, list[0].MedicoId);
    }

    [Fact]
    public async Task Get_Filter_Paciente_200()
    {
        var a = await CreateAsync("P1", "X", pacienteId: 5);
        var b = await CreateAsync("P1", "Y", pacienteId: 5);
        await CreateAsync("P2", "Z", pacienteId: 6); 

        var resp = await _client.GetAsync("/api/Notificacoes?pacienteId=5");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<NotificacaoVm>>();
        Assert.NotNull(list);
        var ids = list!.Select(x => x.Id).ToHashSet();
        Assert.True(ids.Contains(a) && ids.Contains(b));
        Assert.All(list!, x => Assert.Equal(5, x.PacienteId));
    }

    [Fact]
    public async Task GetById_200_E_404()
    {
        var id = await CreateAsync("Sistema", "Ping");

        var ok = await _client.GetAsync($"/api/Notificacoes/{id}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var dto = await ok.Content.ReadFromJsonAsync<NotificacaoVm>();
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);

        var nf = await _client.GetAsync("/api/Notificacoes/999999");
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
    }

    [Fact]
    public async Task Update_Existente_200_ComTrim()
    {
        var id = await CreateAsync("Cat", "Old desc");

        var resp = await _client.PutAsJsonAsync($"/api/Notificacoes/{id}", new
        {
            categoria = "  NovaCat ",
            descricao = "  Nova descricao "
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<NotificacaoVm>();
        Assert.NotNull(dto);
        Assert.Equal("NovaCat", dto!.Categoria);
        Assert.Equal("Nova descricao", dto.Descricao);

        var get = await _client.GetAsync($"/api/Notificacoes/{id}");
        var after = await get.Content.ReadFromJsonAsync<NotificacaoVm>();
        Assert.NotNull(after);
        Assert.Equal("NovaCat", after!.Categoria);
    }

    [Fact]
    public async Task Update_Inexistente_404()
    {
        var resp = await _client.PutAsJsonAsync("/api/Notificacoes/999999", new
        {
            categoria = "X"
        });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Patch_MarcarComoLida_204_E_404()
    {
        var id = await CreateAsync("Ping", "Pong");

        var ok = await _client.PatchAsync($"/api/Notificacoes/{id}/ler?Lida=true", null);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        var get = await _client.GetAsync($"/api/Notificacoes/{id}");
        var dto = await get.Content.ReadFromJsonAsync<NotificacaoVm>();
        Assert.NotNull(dto);
        Assert.True(dto!.Lida);

        var nf = await _client.PatchAsync("/api/Notificacoes/999999/ler?Lida=false", null);
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
    }

    [Fact]
    public async Task Delete_204_E_GetDepois_404()
    {
        var id = await CreateAsync("ToDel", "del me");

        var del = await _client.DeleteAsync($"/api/Notificacoes/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var nf = await _client.GetAsync($"/api/Notificacoes/{id}");
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Notificacoes.AnyAsync(n => n.Id == id));
    }
}
