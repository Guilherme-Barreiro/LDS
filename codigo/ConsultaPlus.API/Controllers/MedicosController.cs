using ConsultaPlus.API.DTOs.Medicos;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Create([FromBody] CreateMedicoDto dto)
        {
            var medico = new Medico
            {
                NomeCompleto = dto.NomeCompleto,
                Telemovel = dto.Telemovel,
                Email = dto.Email,
                NUtente = dto.NUtente,
                PasswordHash = dto.Password, // TODO: hash a sério
                DataNascimento = dto.DataNascimento
            };
            await _repo.AddAsync(medico);
            return CreatedAtAction(nameof(GetById), new
            {
                id = medico.Id
            }, new
            {
                medico.Id,
                medico.NomeCompleto,
                medico.Telemovel,
                medico.Email,
                medico.NUtente,
                medico.DataNascimento,
                medico.DataCriacao
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicoDto dto)
        {
            var medico = await _repo.GetByIdAsync(id);
            if (medico is null) return NotFound();

            medico.NomeCompleto = dto.NomeCompleto;
            medico.Telemovel = dto.Telemovel;
            medico.Email = dto.Email;
            medico.DataNascimento = dto.DataNascimento;

            await _repo.UpdateAsync(medico);
            return NoContent(); // 204
        }


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}