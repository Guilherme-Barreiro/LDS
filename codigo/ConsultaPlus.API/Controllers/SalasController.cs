using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Salas;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalasController : ControllerBase
    {
        private readonly ISalaRepository _repo;
        public SalasController(ISalaRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAllAsync();
            var res = list.Select(s => new SalaResponseDto { Id = s.Id, Nome = s.Nome });
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s is null) return NotFound();
            return Ok(new SalaResponseDto { Id = s.Id, Nome = s.Nome });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSalaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                return BadRequest("Nome da sala é obrigatório.");

            var sala = new Sala { Nome = dto.Nome.Trim() };
            await _repo.AddAsync(sala);

            var res = new SalaResponseDto { Id = sala.Id, Nome = sala.Nome };
            return CreatedAtAction(nameof(GetById), new { id = sala.Id }, res);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
