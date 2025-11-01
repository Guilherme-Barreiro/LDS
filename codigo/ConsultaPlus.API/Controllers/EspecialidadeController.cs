using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeController : ControllerBase
    {
        private readonly IEspecialidadeCRUD _especialidades;

        public EspecialidadeController(IEspecialidadeCRUD especialidades)
        {
            _especialidades = especialidades;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _especialidades.GetAllAsync();
            return Ok(list.Select(e => new { e.Id, e.Nome }));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var esp = await _especialidades.GetByIdAsync(id);
            if (esp is null)
                return NotFound(new { message = $"Especialidade {id} não encontrada." });

            return Ok(new EspecialidadeDTO { Id = esp.Id, Nome = esp.Nome });
        }

        [HttpGet("nome/{nome}")]
        public async Task<IActionResult> GetByNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            var list = await _especialidades.GetAllAsync();

            var results = list
                .Where(e => !string.IsNullOrEmpty(e.Nome) &&
                            e.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase))
                .Select(e => new EspecialidadeDTO { Id = e.Id, Nome = e.Nome })
                .ToList();

            if (results.Count == 0)
                return NotFound(new { message = $"Nenhuma especialidade com nome contendo '{nome}'." });

            return Ok(results);
        }

        [HttpPost]
        public async Task<IActionResult> RegistarEspecialidade(EspecialidadeDTO requestDto)
        {
            var nova = new Especialidade { Nome = requestDto.Nome };
            await _especialidades.AddAsync(nova);
            return CreatedAtAction(nameof(GetAll), new { id = nova.Id }, new { nova.Id, nova.Nome });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var exists = await _especialidades.GetByIdAsync(id);
            if (exists is null)
                return NotFound(new { message = $"Especialidade {id} não existe." });

            await _especialidades.DeleteAsync(id);
            return NoContent();
        }
    }
}
