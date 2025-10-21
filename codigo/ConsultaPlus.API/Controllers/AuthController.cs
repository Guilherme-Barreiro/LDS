using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("registo-paciente")]
        public async Task<IActionResult> Register(RegisterPacienteDto requestDto)
        {
            try
            {
                // Mapeamento de DTO para Modelo
                var novoPaciente = new Paciente
                {
                    NomeCompleto = requestDto.NomeCompleto,
                    NUtente = requestDto.NUtente,
                    Email = requestDto.Email,
                    Nif = requestDto.Nif,
                    Telemovel = requestDto.Telemovel,
                    Morada = requestDto.Morada,
                    DataNascimento = requestDto.DataNascimento
                };

                await _authService.RegisterPacienteAsync(novoPaciente, requestDto.Password);

                return StatusCode(201, "Paciente registado com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Aqui virá o endpoint de Login 
    }
}