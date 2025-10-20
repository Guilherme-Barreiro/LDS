using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using System;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IPacienteRepository _pacienteRepository;

    public AuthService(IPacienteRepository pacienteRepository)
    {
        _pacienteRepository = pacienteRepository;
    }

    public async Task RegisterPacienteAsync(string nomeCompleto, string nUtente, string password, string email)
    {
        // 1. Verificar se o utilizador já existe
        var existingUser = await _pacienteRepository.GetByNUtenteAsync(nUtente);
        if (existingUser != null)
        {
            throw new Exception("Um utilizador com este número de utente já existe.");
        }

        // 2. Fazer o hash da password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // 3. Criar o novo paciente
        var novoPaciente = new Paciente
        {
            NomeCompleto = nomeCompleto,
            NUtente = nUtente,
            PasswordHash = passwordHash,
            Email = email,
            // Preencher os outros campos necessários
        };

        // 4. Guardar na base de dados
        await _pacienteRepository.AddAsync(novoPaciente);
    }
}