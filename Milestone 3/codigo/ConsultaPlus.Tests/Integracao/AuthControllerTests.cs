using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Auth;
public class AuthControllerIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public AuthControllerIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private sealed class TokenRes { public string? Token { get; set; } public string? Message { get; set; } }
    private sealed class ForgotRes { public string? ResetToken { get; set; } public string? Message { get; set; } }


    [Fact]
    public async Task Auth_Flow_Login_Forgot_Reset_LoginAgain_Logout()
    {
        var regBody = new
        {
            nUtente = "123456789",
            password = "1234",
            nomeCompleto = "Paciente Teste",
            nif = "999999990",
            telemovel = "912345678",
            morada = "Rua X",
            email = "paciente@test.local",
            dataNascimento = DateTime.UtcNow.AddYears(-30)
        };
        var reg = await _client.PostAsJsonAsync("/api/Pacientes", regBody);
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);

        var login1 = await _client.PostAsJsonAsync("/api/Auth/login",
            new { nUtente = "123456789", password = "1234" });
        Assert.Equal(HttpStatusCode.OK, login1.StatusCode);
        var t1 = await login1.Content.ReadFromJsonAsync<TokenRes>(_json);
        Assert.False(string.IsNullOrWhiteSpace(t1!.Token));

        var forgot = await _client.PostAsJsonAsync("/api/Auth/forgot-password",
            new { identifier = "123456789" });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        var fRes = await forgot.Content.ReadFromJsonAsync<ForgotRes>(_json);
        Assert.False(string.IsNullOrWhiteSpace(fRes!.ResetToken));

        var reset = await _client.PostAsJsonAsync("/api/Auth/reset-password",
            new { token = fRes.ResetToken!, newPassword = "NovaSenha123!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var login2 = await _client.PostAsJsonAsync("/api/Auth/login",
            new { nUtente = "123456789", password = "NovaSenha123!" });
        Assert.Equal(HttpStatusCode.OK, login2.StatusCode);
        var t2 = await login2.Content.ReadFromJsonAsync<TokenRes>(_json);
        Assert.False(string.IsNullOrWhiteSpace(t2!.Token));

        var reqLogout = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout");
        reqLogout.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t2.Token);
        var logout = await _client.SendAsync(reqLogout);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
    }

    [Fact]
    public async Task Login_ComPasswordErrada_Devolve401()
    {
        await _client.PostAsJsonAsync("/api/Pacientes", new
        {
            nUtente = "111111111",
            password = "abcd",
            nomeCompleto = "P1",
            email = "p1@x",
            nif = "1",
            telemovel = "9",
            morada = "x",
            dataNascimento = DateTime.UtcNow.AddYears(-20)
        });

        var resp = await _client.PostAsJsonAsync("/api/Auth/login",
            new { nUtente = "111111111", password = "ERRADA" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Forgot_Desconhecido_Devolve200_SemToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/Auth/forgot-password",
            new { identifier = "nao-existe" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payload = await resp.Content.ReadFromJsonAsync<ForgotRes>(_json);
        Assert.NotNull(payload);
        Assert.True(string.IsNullOrWhiteSpace(payload!.ResetToken));
    }

    [Fact]
    public async Task Reset_ComTokenInvalido_Devolve400()
    {
        var resp = await _client.PostAsJsonAsync("/api/Auth/reset-password",
            new { token = "xxx.yyy.zzz", newPassword = "Nova123!" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
