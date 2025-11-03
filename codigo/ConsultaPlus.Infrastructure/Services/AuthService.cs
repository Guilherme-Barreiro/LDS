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
        private readonly IConfiguration _configuration; // Variável para guardar a configuração

        //recebe o  IConfiguration
        public AuthService(IPacienteRepository pacienteRepository, IConfiguration configuration)
        {
            _pacienteRepository = pacienteRepository;
            _configuration = configuration;
        }

        //metodo de registo
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

        // Método de Login 
        public async Task<string> LoginAsync(string nUtente, string password)
        {
            // Encontrar o utilizador na base de dados
            var paciente = await _pacienteRepository.GetByNUtenteAsync(nUtente);
            if (paciente == null)
            {
                
                throw new Exception("Número de utente ou password inválidos.");
            }

            //  Verificar a password
            if (!BCrypt.Net.BCrypt.Verify(password, paciente.PasswordHash))
            {
                throw new Exception("Número de utente ou password inválidos.");
            }

            // gera e retorna um  token JWT
            return GenerateJwtToken(paciente);
        }

        // Método privado para gerar o token
        private string GenerateJwtToken(Paciente paciente)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Lê a chave secreta do appsettings.json
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