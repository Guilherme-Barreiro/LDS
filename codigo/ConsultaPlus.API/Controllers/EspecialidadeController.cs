using ConsultaPlus.API.DTOs;
using ConsultaPlus.API.DTOs.Especialidade;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeController : ControllerBase
    {
        private readonly IEspecialidadesService _svc;
        public EspecialidadeController(IEspecialidadesService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok((await _svc.GetAllAsync()).Select(e => new { e.Id, e.Nome }));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var esp = await _svc.GetByIdAsync(id);
            return esp is null
                ? NotFound(new { message = $"Especialidade {id} nao encontrada." })
                : Ok(new EspecialidadeDTO { Id = esp.Id, Nome = esp.Nome });
        }

        [HttpGet("nome/{nome}")]
        public async Task<IActionResult> GetByNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return BadRequest(new { message = "Nome obrigatorio." });

            var results = (await _svc.SearchAsync(nome))
                .Select(e => new EspecialidadeDTO { Id = e.Id, Nome = e.Nome })
                .ToList();

            return results.Count == 0
                ? NotFound(new { message = $"Nenhuma especialidade com nome contendo '{nome}'." })
                : Ok(results);
        }

        [HttpPost]
        public async Task<IActionResult> RegistarEspecialidade([FromBody] EspecialidadeDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            var id = await _svc.CreateAsync(request.Nome.Trim());

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new EspecialidadeDTO { Id = id, Nome = request.Nome.Trim() }
            );
        }


        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, EspecialidadeDTO dto)
        {
            try { await _svc.UpdateAsync(id, dto.Nome); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _svc.DeleteAsync(id); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}
