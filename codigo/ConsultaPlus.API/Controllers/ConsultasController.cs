using ConsultaPlus.API.DTOs.Consultas;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IConsultaService _service;
        private readonly ApplicationDbContext _context;

        public ConsultasController(IConsultaService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            var res = list.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _service.GetByIdAsync(id);
            if (c is null) return NotFound(new { message = $"Consulta {id} não encontrada." });
            return Ok(ToResponse(c));
        }

        [HttpGet("medico/{medicoId:int}")]
        public async Task<IActionResult> GetByMedico(int medicoId)
        {
            var list = await _service.GetByMedicoAsync(medicoId);
            var res = list.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("paciente/{pacienteId:int}")]
        public async Task<IActionResult> GetByPaciente(int pacienteId)
        {
            var list = await _service.GetByPacienteAsync(pacienteId);
            var res = list.Select(ToResponse);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConsultaDto dto)
        {
            try
            {
                var salaId = await _context.Salas
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                if (salaId == 0)
                    return BadRequest(new { message = "Nenhuma sala disponível para associar à consulta." });

                var nova = new Consulta
                {
                    PacienteId = dto.PacienteId,
                    MedicoId = dto.MedicoId,
                    EspecialidadeId = dto.EspecialidadeId,
                    SalaId = salaId,
                    DataConsulta = dto.DataConsulta,
                    Duracao = 30,
                    Estado = "Confirmada"
                };

                var c = await _service.CreateAsync(nova);
                return CreatedAtAction(nameof(GetById), new { id = c.Id }, ToResponse(c));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // api/consultas/{id}/cancelar/paciente
        [HttpPost("{id:int}/cancelar/paciente")]
        public async Task<IActionResult> CancelarPorPaciente(int id, [FromQuery] int pacienteId)
        {
            try
            {
                var ok = await _service.CancelByPacienteAsync(id, pacienteId);
                return ok ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // api/consultas/{id}/cancelar/medico
        [HttpPost("{id:int}/cancelar/medico")]
        public async Task<IActionResult> CancelarPorMedico(int id, [FromQuery] int medicoId)
        {
            try
            {
                var ok = await _service.CancelByMedicoAsync(id, medicoId);
                return ok ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // api/consultas/{id}/atraso/medico?medicoId=3002
        [HttpPost("{id:int}/atraso/medico")]
        public async Task<IActionResult> AtrasoMedico(int id, [FromQuery] int medicoId)
        {
            try
            {
                var ok = await _service.MarkLateByMedicoAsync(id, medicoId);
                return ok ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // api/consultas/{id}/atraso/paciente?pacienteId=2
        [HttpPost("{id:int}/atraso/paciente")]
        public async Task<IActionResult> AtrasoPaciente(int id, [FromQuery] int pacienteId)
        {
            try
            {
                var ok = await _service.MarkLateByPacienteAsync(id, pacienteId);
                return ok ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        private static ConsultaResponseDto ToResponse(Consulta c) => new()
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
    }
}
