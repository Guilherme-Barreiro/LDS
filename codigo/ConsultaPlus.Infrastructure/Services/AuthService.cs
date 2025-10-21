using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using System;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IPacienteRepository _pacienteRepository;

        public AuthService(IPacienteRepository pacienteRepository)
        {
            _pacienteRepository = pacienteRepository;
        }

        // MÉTODO DE REGISTO 
        public async Task RegisterPacienteAsync(Paciente novoPaciente, string password)
        {
            // 1. Verificar se o utilizador já existe
            var existingUser = await _pacienteRepository.GetByNUtenteAsync(novoPaciente.NUtente);
            if (existingUser != null)
            {
                throw new Exception("Um utilizador com este número de utente já existe.");
            }

            // 2. Fazer o hash da password
            novoPaciente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // 3. Preencher outros campos
            novoPaciente.DataCriacao = DateTime.UtcNow;

            // 4. Guardar na base de dados
            await _pacienteRepository.AddAsync(novoPaciente);
        }

        // Implementação do método LoginAsync
        public async Task<string> LoginAsync(string nUtente, string password)
        {
          
            await Task.CompletedTask; 
            throw new NotImplementedException();
        }
    }
}