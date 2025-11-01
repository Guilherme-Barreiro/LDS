using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.API.DTOs.Salas;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalasController : ControllerBase
    {
        private readonly ISalasService _svc;
        public SalasController(ISalasService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _svc.GetAllAsync();
            var res = list.Select(s => new SalaResponseDto { Id = s.Id, Nome = s.Nome });
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var s = await _svc.GetByIdAsync(id);
            return s is null
                ? NotFound()
                : Ok(new SalaResponseDto { Id = s.Id, Nome = s.Nome });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByNome([FromQuery] string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return BadRequest("Parâmetro 'nome' é obrigatório para pesquisa.");

            var list = await _svc.SearchAsync(nome);
            var res = list.Select(s => new SalaResponseDto { Id = s.Id, Nome = s.Nome });
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSalaDto dto)
        {
            try
            {
                var id = await _svc.CreateAsync(dto.Nome);
                return CreatedAtAction(nameof(GetById), new { id }, new SalaResponseDto { Id = id, Nome = dto.Nome.Trim() });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _svc.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}
