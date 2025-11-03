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
        private readonly IMedicoRepository _medicoRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IPacienteRepository pacienteRepository, IMedicoRepository medicoRepository, IConfiguration configuration)
        {
            _pacienteRepository = pacienteRepository;
            _medicoRepository = medicoRepository;
            _configuration = configuration;
        }

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
            // Tenta encontrar como Paciente
            var paciente = await _pacienteRepository.GetByNUtenteAsync(nUtente);
            if (paciente != null)
            {
                if (BCrypt.Net.BCrypt.Verify(password, paciente.PasswordHash))
                {
                    return GenerateJwtToken(paciente.Id.ToString(), paciente.Email, "Paciente");
                }
            }

            // Se não for paciente, tenta como Médico
            var medico = await _medicoRepository.GetByNUtenteAsync(nUtente);
            if (medico != null)
            {
                if (BCrypt.Net.BCrypt.Verify(password, medico.PasswordHash))
                {
                    return GenerateJwtToken(medico.Id.ToString(), medico.Email, "Medico");
                }
            }

            // Se não encontrou ninguém ou a password estava errada
            throw new Exception("Número de utente ou password inválidos.");
        }

        public async Task ForgotPasswordAsync(string email)
        {
            // Tenta encontrar como Paciente
            var paciente = await _pacienteRepository.GetByEmailAsync(email);
            if (paciente != null)
            {
                paciente.PasswordResetToken = Guid.NewGuid().ToString("N");
                paciente.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
                await _pacienteRepository.UpdateAsync(paciente);
                Console.WriteLine($"EMAIL SIMULADO para Paciente {email}: Token {paciente.PasswordResetToken}");
                return;
            }

            // Se não é paciente, tenta como Médico
            var medico = await _medicoRepository.GetByEmailAsync(email);
            if (medico != null)
            {
                medico.PasswordResetToken = Guid.NewGuid().ToString("N");
                medico.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
                await _medicoRepository.UpdateAsync(medico);
                Console.WriteLine($"EMAIL SIMULADO para Medico {email}: Token {medico.PasswordResetToken}");
                return;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            // Tenta encontrar e atualizar como Paciente
            var paciente = await _pacienteRepository.GetByEmailAsync(email);
            if (paciente != null && paciente.PasswordResetToken == token && paciente.ResetTokenExpires >= DateTime.UtcNow)
            {
                paciente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                paciente.PasswordResetToken = null;
                paciente.ResetTokenExpires = null;
                await _pacienteRepository.UpdateAsync(paciente);
                return true;
            }

            // Se não era um paciente, tenta como Médico
            var medico = await _medicoRepository.GetByEmailAsync(email);
            if (medico != null && medico.PasswordResetToken == token && medico.ResetTokenExpires >= DateTime.UtcNow)
            {
                medico.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                medico.PasswordResetToken = null;
                medico.ResetTokenExpires = null;
                await _medicoRepository.UpdateAsync(medico);
                return true;
            }

            return false; // Retorna falso se o token/email for inválido para qualquer perfil
        }

        private string GenerateJwtToken(string userId, string email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"]);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role)
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