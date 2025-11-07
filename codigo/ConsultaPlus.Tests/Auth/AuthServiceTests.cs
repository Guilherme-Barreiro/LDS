using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace ConsultaPlus.Tests.Auth
{
    public class AuthServiceTests
    {
        private readonly Mock<IPacienteRepository> _pacientes = new();
        private readonly Mock<IMedicoRepository> _medicos = new();
        private readonly Mock<ITokenBlacklist> _blacklist = new();
        private readonly IConfiguration _cfg;

        public AuthServiceTests()
        {
            _cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Secret"] = "TEST_SECRET_123456789012345678901234567890",
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["AdminLogin:User"] = "admin",
                    ["AdminLogin:Password"] = "admin"
                })
                .Build();
        }

        private AuthService CreateSut() =>
            new AuthService(_pacientes.Object, _medicos.Object, _blacklist.Object, _cfg);


        [Fact]
        public async Task Login_Admin_By_Config_Works()
        {
            var sut = CreateSut();

            var jwt = await sut.LoginAsync("admin", "admin");

            Assert.False(string.IsNullOrWhiteSpace(jwt));
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }

        [Fact]
        public async Task Login_Paciente_By_NUtente_Works()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("1234");
            _pacientes.Setup(r => r.GetByNUtenteAsync("999"))
                      .ReturnsAsync(new Paciente { Id = 7, NUtente = "999", Email = "p@x", PasswordHash = hash });

            var sut = CreateSut();

            var jwt = await sut.LoginAsync("999", "1234");

            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Paciente");
            Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "7");
        }

        [Fact]
        public async Task Login_Medico_By_NUtente_Works()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("m123");
            _medicos.Setup(r => r.GetByNUtenteAsync("777"))
                    .ReturnsAsync(new Medico { Id = 3, NUtente = "777", Email = "m@x", PasswordHash = hash });

            var sut = CreateSut();

            var jwt = await sut.LoginAsync("777", "m123");

            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Medico");
            Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "3");
        }

        [Fact]
        public async Task Login_Invalid_Throws()
        {
            _pacientes.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>()))
                      .ReturnsAsync((Paciente?)null);
            _medicos.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>()))
                    .ReturnsAsync((Medico?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<Exception>(() => sut.LoginAsync("nope", "nope"));
        }


        [Fact]
        public async Task Logout_Blacklists_Jti_Until_Exp()
        {
            var handler = new JwtSecurityTokenHandler();
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["JwtSettings:Secret"]!)),
                SecurityAlgorithms.HmacSha256);
            var jti = Guid.NewGuid().ToString();

            var token = handler.CreateEncodedJwt(
                issuer: _cfg["JwtSettings:Issuer"],
                audience: _cfg["JwtSettings:Audience"],
                subject: new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, jti) }),
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(5),
                issuedAt: DateTime.UtcNow,
                signingCredentials: creds);

            var sut = CreateSut();

            await sut.LogoutAsync("Bearer " + token);

            _blacklist.Verify(b => b.Add(It.Is<string>(x => x == jti),
                                         It.IsAny<DateTime>()),
                              Times.Once);
        }


        [Fact]
        public async Task Forgot_For_Paciente_By_NUtente_Returns_ResetToken()
        {
            var p = new Paciente { Id = 10, NUtente = "123456789", Email = "p@x", PasswordHash = "h" };
            _pacientes.Setup(r => r.GetByNUtenteAsync("123456789")).ReturnsAsync(p);

            var sut = CreateSut();

            var resetToken = await sut.CreatePasswordResetAsync("123456789");

            Assert.False(string.IsNullOrWhiteSpace(resetToken));
            var t = new JwtSecurityTokenHandler().ReadJwtToken(resetToken);
            Assert.Equal("PasswordReset", t.Audiences is null ? null : string.Join(',', t.Audiences));
            Assert.Contains(t.Claims, c => c.Type == "purpose" && c.Value == "password_reset");
        }

        [Fact]
        public async Task Forgot_For_Paciente_By_Email_Returns_ResetToken()
        {
            _pacientes.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>())).ReturnsAsync((Paciente?)null);
            _pacientes.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(new[] { new Paciente { Id = 11, Email = "p@x.com" } });

            var sut = CreateSut();

            var token = await sut.CreatePasswordResetAsync("p@x.com");

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task Forgot_For_Medico_By_NUtente_Returns_ResetToken()
        {
            _pacientes.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>())).ReturnsAsync((Paciente?)null);
            _medicos.Setup(r => r.GetByNUtenteAsync("777")).ReturnsAsync(new Medico { Id = 22, NUtente = "777" });

            var sut = CreateSut();

            var token = await sut.CreatePasswordResetAsync("777");

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task Forgot_For_Medico_By_Email_Returns_ResetToken()
        {
            _pacientes.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>())).ReturnsAsync((Paciente?)null);
            _medicos.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>())).ReturnsAsync((Medico?)null);
            _medicos.Setup(r => r.GetByEmailAsync("m@x.com")).ReturnsAsync(new Medico { Id = 33, Email = "m@x.com" });

            var sut = CreateSut();

            var token = await sut.CreatePasswordResetAsync("m@x.com");

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public async Task Forgot_Unknown_Returns_Empty()
        {
            _pacientes.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>())).ReturnsAsync((Paciente?)null);
            _pacientes.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Paciente>());
            _medicos.Setup(r => r.GetByNUtenteAsync(It.IsAny<string>())).ReturnsAsync((Medico?)null);
            _medicos.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Medico?)null);

            var sut = CreateSut();

            var token = await sut.CreatePasswordResetAsync("nao-existe");

            Assert.True(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task Reset_For_Paciente_Updates_Hash()
        {
            var p = new Paciente { Id = 88, PasswordHash = "old" };
            _pacientes.Setup(r => r.GetByIdAsync(88)).ReturnsAsync(p);

            var sut = CreateSut();
            _pacientes.Setup(r => r.GetByNUtenteAsync("A")).ReturnsAsync(new Paciente { Id = 88, Email = "e" });
            var reset = await sut.CreatePasswordResetAsync("A");

            await sut.ResetPasswordAsync(reset, "Nova123");

            _pacientes.Verify(r => r.UpdateAsync(It.Is<Paciente>(x => x.Id == 88 && x.PasswordHash != "old")), Times.Once);
        }

        [Fact]
        public async Task Reset_For_Medico_Updates_Hash()
        {
            var m = new Medico { Id = 99, PasswordHash = "old", NUtente = "777" };
            _medicos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(m);
            _medicos.Setup(r => r.GetByNUtenteAsync("777")).ReturnsAsync(new Medico { Id = 99, NUtente = "777" });

            var sut = CreateSut();
            var reset = await sut.CreatePasswordResetAsync("777");

            await sut.ResetPasswordAsync(reset, "Nova123");

            _medicos.Verify(r => r.UpdateAsync(It.Is<Medico>(x => x.Id == 99 && x.PasswordHash != "old")), Times.Once);
        }

        [Fact]
        public async Task Reset_With_Invalid_Token_Throws()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<SecurityTokenException>(() => sut.ResetPasswordAsync("xxx.yyy.zzz", "pwd"));
        }
    }
}
