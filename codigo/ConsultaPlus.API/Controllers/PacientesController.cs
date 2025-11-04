using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Pacientes;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly IPacienteRepository _repo;
        public PacientesController(IPacienteRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAllAsync();
            var res = list.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _repo.GetByIdAsync(id);
            return p is null ? NotFound() : Ok(ToResponse(p));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePacienteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NUtente))
                return BadRequest(new { message = "NUtente e obrigatorio." });
            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Password e obrigatoria." });

            var exists = await _repo.GetByNUtenteAsync(dto.NUtente.Trim());
            if (exists != null) return Conflict(new { message = "Ja existe paciente com este NUtente." });

            var entity = new Paciente
            {
                NUtente = dto.NUtente.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                NomeCompleto = dto.NomeCompleto?.Trim(),
                Nif = dto.Nif?.Trim(),
                Telemovel = dto.Telemovel?.Trim(),
                Morada = dto.Morada?.Trim(),
                Email = dto.Email?.Trim(),
                DataNascimento = dto.DataNascimento
            };

            await _repo.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePacienteDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity is null) return NotFound();

            entity.NomeCompleto = dto.NomeCompleto ?? entity.NomeCompleto;
            entity.Nif = dto.Nif ?? entity.Nif;
            entity.Telemovel = dto.Telemovel ?? entity.Telemovel;
            entity.Morada = dto.Morada ?? entity.Morada;
            entity.Email = dto.Email ?? entity.Email;
            if (dto.DataNascimento.HasValue) entity.DataNascimento = dto.DataNascimento.Value;

            await _repo.UpdateAsync(entity);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        private static PacienteResponseDto ToResponse(Paciente p) => new()
        {
            Id = p.Id,
            NomeCompleto = p.NomeCompleto,
            Nif = p.Nif,
            NUtente = p.NUtente,
            Telemovel = p.Telemovel,
            Morada = p.Morada,
            Email = p.Email,
            DataNascimento = p.DataNascimento,
            DataCriacao = p.DataCriacao
        };
    }
}
