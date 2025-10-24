using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicosController : ControllerBase
    {
        private readonly IMedicoRepository _repo;

        public MedicosController(IMedicoRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var medico = await _repo.GetByIdAsync(id);
            return medico is null ? NotFound() : Ok(medico);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Medico medico)
        {
            // NOTA: em produção não aceites PasswordHash no payload.
            await _repo.AddAsync(medico);
            return CreatedAtAction(nameof(GetById), new { id = medico.Id }, medico);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Medico medico)
        {
            if (id != medico.Id) return BadRequest("Id mismatch");
            await _repo.UpdateAsync(medico);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
