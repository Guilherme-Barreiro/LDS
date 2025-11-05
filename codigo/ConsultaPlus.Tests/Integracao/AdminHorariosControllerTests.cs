using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.HorarioMedico;
public class AdminHorariosControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;
    public record HorarioVm(int Id, int MedicoId, string DiaSemana, string HoraInicio, string HoraFim);
    public record ExcecaoVm(int Id, int MedicoId, string Data, string HoraInicio, string HoraFim, bool IsReducao, string? Motivo);


    public AdminHorariosControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> GetMedicoIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = await db.Medicos.Select(m => m.Id).FirstOrDefaultAsync();
        if (id == 0)
        {
            db.Medicos.Add(new ConsultaPlus.Core.Models.Medico
            {
                NomeCompleto = "Dr Teste",
                Email = "dr@x.com",
                Telemovel = "900000000",
                NUtente = Guid.NewGuid().ToString("N").Substring(0, 9),
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-40)
            });
            await db.SaveChangesAsync();
            id = await db.Medicos.Select(m => m.Id).FirstAsync();
        }
        return id;
    }

    [Fact]
    public async Task Post_Horario_Valido_Devolve204()
    {
        var medicoId = await GetMedicoIdAsync();
        var body = new { diaSemana = "Seg", horaInicio = "09:00:00", horaFim = "12:00:00" };

        var resp = await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario", body);

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Post_Horario_Sobreposicao_Devolve409()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Ter", horaInicio = "09:00:00", horaFim = "12:00:00" });

        var resp = await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Ter", horaInicio = "11:00:00", horaFim = "13:00:00" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Get_Horarios_Devolve200_ComLista()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Qua", horaInicio = "10:00:00", horaFim = "12:00:00" });

        var resp = await _client.GetAsync($"/api/admin/medicos/{medicoId}/horario");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var lista = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(lista);
        Assert.True(lista!.Count >= 1);
    }

    [Fact]
    public async Task Put_Horario_Atualiza_Devolve200()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Qui", horaInicio = "09:00:00", horaFim = "11:00:00" });

        int horarioId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            horarioId = await db.HorariosTrabalhoMedicos
                .Where(h => h.MedicoId == medicoId && h.DiaSemana == "Qui")
                .OrderByDescending(h => h.Id)
                .Select(h => h.Id)
                .FirstAsync();
        }

        var resp = await _client.PutAsJsonAsync($"/api/admin/medicos/{medicoId}/horario/{horarioId}",
            new { diaSemana = "qui", horaInicio = "08:30:00", horaFim = "11:30:00" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<HorarioVm>();
        Assert.NotNull(body);
        Assert.Equal("Qui", body!.DiaSemana);
        Assert.Equal("08:30:00", body.HoraInicio);
        Assert.Equal("11:30:00", body.HoraFim);
    }

    [Fact]
    public async Task Get_Excecoes_Devolve200_ComFiltro()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes",
            new { data = "2025-10-27", horaInicio = "10:00:00", horaFim = "12:00:00", isReducao = true, motivo = "Formação" });
        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes",
            new { data = "2025-10-28", horaInicio = "09:00:00", horaFim = "10:00:00", isReducao = false });

        var resp = await _client.GetAsync($"/api/admin/medicos/{medicoId}/excecoes?data=2025-10-27");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var lista = await resp.Content.ReadFromJsonAsync<List<ExcecaoVm>>();
        Assert.NotNull(lista);
        Assert.True(lista!.Count >= 1);
        Assert.All(lista!, e => Assert.Equal("2025-10-27", e.Data));
    }


    [Fact]
    public async Task Delete_Horario_204_EDepois_404()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Sex", horaInicio = "15:00:00", horaFim = "16:00:00" });

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            id = await db.HorariosTrabalhoMedicos
                .Where(h => h.MedicoId == medicoId && h.DiaSemana == "Sex")
                .Select(h => h.Id).FirstAsync();
        }

        var del = await _client.DeleteAsync($"/api/admin/medicos/{medicoId}/horario/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var again = await _client.DeleteAsync($"/api/admin/medicos/{medicoId}/horario/{id}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Get_Horario_ById_200_E_Depois_404()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Seg", horaInicio = "08:00:00", horaFim = "09:00:00" });

        int horarioId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            horarioId = await db.HorariosTrabalhoMedicos
                .Where(h => h.MedicoId == medicoId && h.DiaSemana == "Seg")
                .OrderByDescending(h => h.Id)
                .Select(h => h.Id)
                .FirstAsync();
        }

        var ok = await _client.GetAsync($"/api/admin/medicos/{medicoId}/horario/{horarioId}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var nf = await _client.GetAsync($"/api/admin/medicos/{medicoId + 1}/horario/{horarioId}");
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
    }

    [Fact]
    public async Task Get_Excecao_ById_200_E_404()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes",
            new { data = "2025-10-30", horaInicio = "09:00:00", horaFim = "10:00:00", isReducao = true, motivo = "Teste" });

        int excId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            excId = await db.HorariosExcecaoMedicos
                .Where(e => e.MedicoId == medicoId && e.Data == new DateTime(2025, 10, 30))
                .OrderByDescending(e => e.Id)
                .Select(e => e.Id)
                .FirstAsync();
        }

        var ok = await _client.GetAsync($"/api/admin/medicos/{medicoId}/excecoes/{excId}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var nf = await _client.GetAsync($"/api/admin/medicos/{medicoId + 1}/excecoes/{excId}");
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
    }

    [Fact]
    public async Task Post_Horario_HorasInvalidas_400()
    {
        var medicoId = await GetMedicoIdAsync();

        var resp = await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/horario",
            new { diaSemana = "Ter", horaInicio = "12:00:00", horaFim = "11:00:00" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Post_Excecao_HorasInvalidas_400()
    {
        var medicoId = await GetMedicoIdAsync();

        var resp = await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes",
            new { data = "2025-10-31", horaInicio = "10:00:00", horaFim = "09:00:00", isReducao = false });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Put_Excecao_400_404_200()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes",
            new { data = "2025-11-01", horaInicio = "09:00:00", horaFim = "10:00:00", isReducao = true, motivo = "Inicial" });

        int excId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            excId = await db.HorariosExcecaoMedicos
                .Where(e => e.MedicoId == medicoId && e.Data == new DateTime(2025, 11, 1))
                .Select(e => e.Id).FirstAsync();
        }

        var bad = await _client.PutAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes/{excId}",
            new { data = "2025-11-01", horaInicio = "10:00:00", horaFim = "10:00:00", isReducao = true, motivo = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var nf = await _client.PutAsJsonAsync($"/api/admin/medicos/{medicoId + 1}/excecoes/{excId}",
            new { data = "2025-11-02", horaInicio = "08:30:00", horaFim = "09:30:00", isReducao = false, motivo = "y" });
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);

        var ok = await _client.PutAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes/{excId}",
            new { data = "2025-11-02", horaInicio = "08:30:00", horaFim = "09:30:00", isReducao = false, motivo = "Atual" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var dto = await ok.Content.ReadFromJsonAsync<ExcecaoVm>();
        Assert.NotNull(dto);
        Assert.Equal("2025-11-02", dto!.Data);
        Assert.False(dto.IsReducao);
        Assert.Equal("Atual", dto.Motivo);
    }

    [Fact]
    public async Task Delete_Excecao_204_EDepois_404()
    {
        var medicoId = await GetMedicoIdAsync();

        await _client.PostAsJsonAsync($"/api/admin/medicos/{medicoId}/excecoes",
            new { data = "2025-11-03", horaInicio = "14:00:00", horaFim = "15:00:00", isReducao = false });

        int excId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            excId = await db.HorariosExcecaoMedicos
                .Where(e => e.MedicoId == medicoId && e.Data == new DateTime(2025, 11, 3))
                .Select(e => e.Id).FirstAsync();
        }

        var del = await _client.DeleteAsync($"/api/admin/medicos/{medicoId}/excecoes/{excId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var again = await _client.DeleteAsync($"/api/admin/medicos/{medicoId}/excecoes/{excId}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Post_Horario_MedicoInexistente_404()
    {
        var resp = await _client.PostAsJsonAsync($"/api/admin/medicos/{int.MaxValue}/horario",
            new { diaSemana = "Seg", horaInicio = "09:00:00", horaFim = "12:00:00" });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Get_Horarios_SemRegistos_200_Vazio()
    {
        var medicoId = await CreateMedicoNovoAsync();

        var resp = await _client.GetAsync($"/api/admin/medicos/{medicoId}/horario");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var lista = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(lista);
        Assert.Empty(lista!);
    }

    private async Task<int> CreateMedicoNovoAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var m = new ConsultaPlus.Core.Models.Medico
        {
            NomeCompleto = "Dr Novo",
            Email = $"{Guid.NewGuid():N}@x.com",
            Telemovel = "900000000",
            NUtente = Guid.NewGuid().ToString("N").Substring(0, 9),
            PasswordHash = "x",
            DataNascimento = DateTime.UtcNow.AddYears(-40)
        };
        db.Medicos.Add(m);
        await db.SaveChangesAsync();
        return m.Id;
    }

}
