using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IPacienteRepository _pacientes;
        private readonly IMedicoRepository _medicos;
        private readonly IConfiguration _configuration;

        public AuthService(IPacienteRepository pacienteRepository,
                           IMedicoRepository medicoRepository,
                           IConfiguration configuration)
        {
            _pacientes = pacienteRepository;
            _medicos = medicoRepository;
            _configuration = configuration;
        }

        public async Task<string> LoginAsync(string nUtente, string password)
        {
            if (IsAdminCredentials(nUtente, password))
                return GenerateJwtToken(-1, "admin@local", "Admin");

            var paciente = await _pacientes.GetByNUtenteAsync(nUtente);
            if (paciente != null)
            {
                if (!BCrypt.Net.BCrypt.Verify(password, paciente.PasswordHash))
                    throw new Exception("Número de utente ou password inválidos.");

                return GenerateJwtToken(paciente.Id, paciente.Email, "Paciente");
            }

            var medico = await _medicos.GetByNUtenteAsync(nUtente);
            if (medico != null)
            {
                if (!BCrypt.Net.BCrypt.Verify(password, medico.PasswordHash))
                    throw new Exception("Número de utente ou password inválidos.");

                return GenerateJwtToken(medico.Id, medico.Email, "Medico");
            }

            throw new Exception("Número de utente ou password inválidos.");
        }

        private bool IsAdminCredentials(string nUtente, string password)
        {
            var cfgUser = _configuration["AdminLogin:User"] ?? "admin";
            var cfgPass = _configuration["AdminLogin:Password"] ?? "admin";
            return string.Equals(nUtente, cfgUser, StringComparison.Ordinal)
                && string.Equals(password, cfgPass, StringComparison.Ordinal);
        }

        private string GenerateJwtToken(int userId, string? email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email ?? string.Empty),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
