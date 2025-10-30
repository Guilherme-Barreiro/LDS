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
        private readonly IPacienteRepository _pacienteRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IPacienteRepository pacienteRepository, IConfiguration configuration)
        {
            _pacienteRepository = pacienteRepository;
            _configuration = configuration;
        }

        // --- MÉTODOS EXISTENTES (ESTAVAM PERFEITOS) ---

        public async Task RegisterPacienteAsync(Paciente novoPaciente, string password)
        {
            var existingUser = await _pacienteRepository.GetByNUtenteAsync(novoPaciente.NUtente);
            if (existingUser != null)
            {
                throw new Exception("Um utilizador com este número de utente já existe.");
            }
            novoPaciente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            novoPaciente.DataCriacao = DateTime.UtcNow;
            await _pacienteRepository.AddAsync(novoPaciente);
        }

        public async Task<string> LoginAsync(string nUtente, string password)
        {
            var paciente = await _pacienteRepository.GetByNUtenteAsync(nUtente);
            if (paciente == null || !BCrypt.Net.BCrypt.Verify(password, paciente.PasswordHash))
            {
                throw new Exception("Número de utente ou password inválidos.");
            }
            return GenerateJwtToken(paciente);
        }

        // --- NOVOS MÉTODOS PARA RECUPERAÇÃO DE PASSWORD ---

        public async Task ForgotPasswordAsync(string email)
        {
            // Nota: Este método requer que o seu IPacienteRepository tenha um método GetByEmailAsync.
            var paciente = await _pacienteRepository.GetByEmailAsync(email);
            if (paciente == null)
            {
                // Por segurança, não revelamos se o email existe. Agimos como se tudo tivesse corrido bem.
                return;
            }

            // Gerar token de reset e definir expiração
            paciente.PasswordResetToken = Guid.NewGuid().ToString("N"); // "N" para remover hífens
            paciente.ResetTokenExpires = DateTime.UtcNow.AddHours(1); // Validade de 1 hora

            // Nota: Este método requer que o seu IPacienteRepository tenha um método UpdateAsync.
            await _pacienteRepository.UpdateAsync(paciente);

            // TODO: Aqui entraria a chamada a um serviço de envio de email.
            // Por agora, vamos simular escrevendo na consola.
            Console.WriteLine($"EMAIL SIMULADO para {email}: O seu token de reset é {paciente.PasswordResetToken}");
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var paciente = await _pacienteRepository.GetByEmailAsync(email);

            if (paciente == null ||
                paciente.PasswordResetToken != token ||
                paciente.ResetTokenExpires < DateTime.UtcNow)
            {
                // Se o paciente não existe, o token é inválido, ou o token expirou, a operação falha.
                return false;
            }

            // Tudo válido, redefinir a password
            paciente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Limpar os campos de reset para que o token não possa ser reutilizado
            paciente.PasswordResetToken = null;
            paciente.ResetTokenExpires = null;

            await _pacienteRepository.UpdateAsync(paciente);

            return true;
        }

        // --- MÉTODO PRIVADO (ESTAVA PERFEITO) ---
        private string GenerateJwtToken(Paciente paciente)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"]);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, paciente.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, paciente.Email),
                new Claim(ClaimTypes.Role, "Paciente")
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}