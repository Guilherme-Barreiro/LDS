using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminsController : ControllerBase
    {
        private readonly IAdminRepository _admins;
        public AdminsController(IAdminRepository admins) => _admins = admins;

        public record CreateAdminDto(string Username, string Password, string? Email);

        [AllowAnonymous]
        [HttpPost("seed-first-admin")]
        public async Task<IActionResult> SeedFirst(CreateAdminDto dto)
        {
            if (await _admins.AnyAsync())
                return BadRequest(new { message = "Já existe um administrador." });

            var admin = new Admin
            {
                Username = dto.Username.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email!.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _admins.AddAsync(admin);
            return Ok(new { message = "Administrador inicial criado." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateAdminDto dto)
        {
            var admin = new Admin
            {
                Username = dto.Username.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email!.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _admins.AddAsync(admin);
            return Ok(new { message = "Administrador criado." });
        }
    }
}
