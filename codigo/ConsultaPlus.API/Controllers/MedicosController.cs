using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Medicos;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicosController : ControllerBase
    {
        private readonly IMedicoRepository _repo;

        public MedicosController(IMedicoRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var medicos = await _repo.GetAllAsync();
            var res = medicos.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var medico = await _repo.GetByIdAsync(id);
            if (medico is null) return NotFound();

            return Ok(ToResponse(medico));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMedicoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeCompleto))
                return BadRequest("NomeCompleto é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.NUtente))
                return BadRequest("NUtente é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Telemovel))
                return BadRequest("Telemovel é obrigatório.");

            var medico = new Medico
            {
                NomeCompleto = dto.NomeCompleto.Trim(),
                Telemovel = dto.Telemovel.Trim(),
                Email = dto.Email.Trim(),
                NUtente = dto.NUtente.Trim(),
                PasswordHash = dto.Password,
                DataNascimento = dto.DataNascimento
            };

            await _repo.AddAsync(medico);

            var res = ToResponse(medico);
            return CreatedAtAction(nameof(GetById), new { id = medico.Id }, res);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicoDto dto)
        {
            var medico = await _repo.GetByIdAsync(id);
            if (medico is null) return NotFound();

            medico.NomeCompleto = dto.NomeCompleto?.Trim() ?? medico.NomeCompleto;
            medico.Telemovel = dto.Telemovel?.Trim() ?? medico.Telemovel;
            medico.Email = dto.Email?.Trim() ?? medico.Email;
            medico.DataNascimento = dto.DataNascimento;

            await _repo.UpdateAsync(medico);
            return NoContent(); 
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        private static MedicoResponseDto ToResponse(Medico m) => new()
        {
            Id = m.Id,
            NomeCompleto = m.NomeCompleto,
            Telemovel = m.Telemovel,
            Email = m.Email,
            NUtente = m.NUtente,
            DataNascimento = m.DataNascimento,
            DataCriacao = m.DataCriacao
        };
    }
}
