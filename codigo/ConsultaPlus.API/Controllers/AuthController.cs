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
                // Mapeamento de DTO para o Modelo de Domínio
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

                // Chama o serviço para executar 
                await _authService.RegisterPacienteAsync(novoPaciente, requestDto.Password);

                return StatusCode(201, new { message = "Paciente registado com sucesso." });
            }
            catch (Exception ex)
            {
                // Retorna um erro 400 com a mensagem de erro
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            try
            {
                //valida as credenciais e gera um token
                var token = await _authService.LoginAsync(loginDto.NUtente, loginDto.Password);

                // Retorna uma resposta 200 OK com o token
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                // Retorna 401 se o login falhar
                return Unauthorized(new { message = ex.Message });
            }
        }
<<<<<<< Updated upstream
=======

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            try
            {
                
                await _authService.ForgotPasswordAsync(dto.Email);

                // Retornamos sempre OK 
                return Ok(new { message = "Se a sua conta existir, um email foi enviado com as instruções para redefinir a sua password." });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, "Ocorreu um erro interno.");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
                if (!result)
                {
                    return BadRequest(new { message = "Token inválido ou expirado." });
                }
                return Ok(new { message = "Password redefinida com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro interno.");
            }
        }

>>>>>>> Stashed changes
    }
}