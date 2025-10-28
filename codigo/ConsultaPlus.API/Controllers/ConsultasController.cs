// ConsultaPlus.API/Controllers/ConsultasController.cs
using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Consultas;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IConsultaRepository _repo;
        public ConsultasController(IConsultaRepository repo) => _repo = repo;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConsultaDto dto)
        {
            var c = new Consulta
            {
                PacienteId = dto.PacienteId,
                MedicoId = dto.MedicoId,
                SalaId = dto.SalaId,
                EspecialidadeId = dto.EspecialidadeId,
                DataConsulta = dto.DataConsulta,
                Duracao = dto.Duracao,
                Estado = dto.Estado
            };

            await _repo.AddAsync(c);

            var res = new ConsultaResponseDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                MedicoId = c.MedicoId,
                SalaId = c.SalaId,
                EspecialidadeId = c.EspecialidadeId,
                DataConsulta = c.DataConsulta,
                Duracao = c.Duracao,
                Estado = c.Estado
            };

            return StatusCode(201, res);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
