using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // para DbUpdateException
using System.Linq;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeController : ControllerBase
    {
        private readonly IEspecialidadeRepository _especialidades;

        public EspecialidadeController(IEspecialidadeRepository especialidades)
        {
            _especialidades = especialidades;
        }

        // GET: /api/Especialidade
        [HttpGet]
        public async Task<IActionResult> GetTodas()
        {
            var list = await _especialidades.GetAllAsync();

            // resposta enxuta
            var res = list
                .OrderBy(e => e.Nome)
                .Select(e => new { e.Id, e.Nome });

            return Ok(res);
        }

        // POST: /api/Especialidade
        [HttpPost]
        public async Task<IActionResult> RegistarEspecialidade([FromBody] EspecialidadeDTO requestDto)
        {
            var nova = new Especialidade { Nome = requestDto.Nome };
            await _especialidades.AddAsync(nova);

            return StatusCode(201, new { nova.Id, nova.Nome });
        }

        // DELETE: /api/Especialidade/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _especialidades.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = $"Especialidade {id} não encontrada." });

            try
            {
                await _especialidades.DeleteAsync(id);
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                // Provável FK: Consultas ou EspecialidadesMedico
                return Conflict(new
                {
                    message = "Não é possível eliminar a especialidade: existem registos dependentes (médicos/consultas).",
                    detail = ex.Message
                });
            }
        }
    }
}
