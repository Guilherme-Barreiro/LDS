using ConsultaPlus.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IPacienteRepository _pacientes;
        private readonly IMedicoRepository _medicos;
        private readonly IAdminRepository _admins;   
        private readonly IConfiguration _configuration;
        private readonly ITokenBlacklist _blacklist;

        public AuthService(
            IPacienteRepository pacienteRepository,
            IMedicoRepository medicoRepository,
            ITokenBlacklist blacklist,
            IConfiguration configuration,
            IAdminRepository adminRepository)          
        {
            _pacientes = pacienteRepository;
            _medicos = medicoRepository;
            _blacklist = blacklist;
            _configuration = configuration;
            _admins = adminRepository;                 
        }

        public async Task<string> LoginAsync(string nUtente, string password)
        {
            var admin = await _admins.GetByUsernameAsync(nUtente);
            if (admin != null)
            {
                if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
                    throw new Exception("Credenciais inválidas.");
                return GenerateJwtToken(admin.Id, admin.Email, "Admin");
            }

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

            throw new Exception("Credenciais inválidas.");
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

            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            });

            return tokenHandler.WriteToken(token);
        }

        public Task LogoutAsync(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var raw = jwt.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            var token = handler.ReadJwtToken(raw);

            _blacklist.Add(token.Id, token.ValidTo);
            return Task.CompletedTask;
        }

        public async Task<string> CreatePasswordResetAsync(string identifier)
        {
            var paciente = await _pacientes.GetByNUtenteAsync(identifier);
            if (paciente == null && identifier.Contains("@"))
            {
                var all = await _pacientes.GetAllAsync();
                paciente = all.FirstOrDefault(p => string.Equals(p.Email, identifier, StringComparison.OrdinalIgnoreCase));
            }
            if (paciente != null)
                return GeneratePasswordResetToken(paciente.Id, "Paciente");

            var medico = await _medicos.GetByNUtenteAsync(identifier);
            if (medico == null && identifier.Contains("@"))
                medico = await _medicos.GetByEmailAsync(identifier);
            if (medico != null)
                return GeneratePasswordResetToken(medico.Id, "Medico");


            return "";
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            var (userId, role) = ValidatePasswordResetToken(token);
            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            switch (role)
            {
                case "Paciente":
                    var p = await _pacientes.GetByIdAsync(userId) ?? throw new Exception("Utilizador não encontrado.");
                    p.PasswordHash = hash;
                    await _pacientes.UpdateAsync(p);
                    break;

                case "Medico":
                    var m = await _medicos.GetByIdAsync(userId) ?? throw new Exception("Utilizador não encontrado.");
                    m.PasswordHash = hash;
                    await _medicos.UpdateAsync(m);
                    break;

                default:
                    throw new Exception("Operação não suportada para este utilizador.");
            }
        }

        private string GeneratePasswordResetToken(int userId, string role)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("purpose", "password_reset")
            };

            var token = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15), 
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = "PasswordReset",              
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            });

            return handler.WriteToken(token);
        }

        private (int userId, string role) ValidatePasswordResetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new SecurityTokenException("Token ausente.");

            var handler = new JwtSecurityTokenHandler();
            var secret = _configuration["JwtSettings:Secret"]
                         ?? throw new InvalidOperationException("JwtSettings:Secret não está configurado.");
            var key = Encoding.UTF8.GetBytes(secret);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = "PasswordReset",
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, parameters, out _);

            var purpose = principal.FindFirst("purpose")?.Value;
            if (!string.Equals(purpose, "password_reset", StringComparison.Ordinal))
                throw new SecurityTokenException("Token não é de reset de password.");

            var sub =
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new SecurityTokenException("Token sem utilizador (sub).");

            var role = principal.FindFirst(ClaimTypes.Role)?.Value
                       ?? throw new SecurityTokenException("Token sem role.");

            return (int.Parse(sub), role);
        }
    }
}
