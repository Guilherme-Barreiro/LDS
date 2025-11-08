using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Authorization;
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


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            try
            {
                var token = await _authService.LoginAsync(loginDto.NUtente, loginDto.Password);

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            await _authService.LogoutAsync(authHeader);
            return Ok(new { message = "Sessão terminada com sucesso." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> Forgot(ForgotPasswordDto dto)
        {
            try
            {
                var resetToken = await _authService.CreatePasswordResetAsync(dto.Identifier);
                var ok = !string.IsNullOrWhiteSpace(resetToken);
                return Ok(new
                {
                    message = ok
                        ? "Se o identificador existir, foi gerado um token de reset."
                        : "Se o identificador existir, receberás instruções (demo: sem token).",
                    resetToken = resetToken
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> Reset(ResetPasswordDto dto)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
                return Ok(new { message = "Password atualizada com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
