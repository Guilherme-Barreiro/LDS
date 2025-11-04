using System.Linq;
using System.Threading.Tasks;
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

        public ConsultasController(IConsultaRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAllAsync();
            var res = list.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return NotFound(new { message = $"Consulta {id} não encontrada." });
            return Ok(ToResponse(c));
        }

        [HttpGet("medico/{medicoId:int}/consultas")]
        public async Task<IActionResult> GetByMedico(
            int medicoId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] bool onlyConfirmed = false,
            CancellationToken ct = default)
        {
            var start = (from ?? DateTime.UtcNow.Date).Date;          
            var endExclusive = ((to ?? start).Date).AddDays(1);      

            if (endExclusive <= start)
                return BadRequest("'to' deve ser >= 'from'.");

            var list = await _repo.GetByMedicoRangeAsync(medicoId, start, endExclusive, onlyConfirmed, ct);

            var dtos = list.Select(c => new AgendaItemDto(
                c.Id,
                c.DataConsulta,
                c.DataConsulta.AddMinutes(c.Duracao),
                c.Estado,
                c.PacienteId,
                c.SalaId
            ));

            return Ok(dtos);
        }



        [HttpGet("paciente/{pacienteId:int}/consultas")]
        public async Task<IActionResult> GetByPaciente(
            int pacienteId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken ct = default)
        {
            var res = await _repo.GetByPacienteAsync(pacienteId, from, to, page, pageSize, ct);

            var items = res.Items.Select(c => new ConsultaPacienteDto(
                c.Id,
                c.DataConsulta,
                c.DataConsulta.AddMinutes(c.Duracao),
                c.Estado,
                c.MedicoId,
                c.EspecialidadeId,
                c.SalaId
            )).ToList();

            return Ok(new PagedListDto<ConsultaPacienteDto>(res.Total, res.Page, res.PageSize, items));
        }

        [HttpGet("medico/{medicoId:int}")]
        public async Task<IActionResult> GetByMedico(int medicoId)
        {
            var list = await _repo.GetAllAsync();
            var res = list.Where(c => c.MedicoId == medicoId).Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("paciente/{pacienteId:int}")]
        public async Task<IActionResult> GetByPaciente(int pacienteId)
        {
            var list = await _repo.GetAllAsync();
            var res = list.Where(c => c.PacienteId == pacienteId).Select(ToResponse);
            return Ok(res);
        }


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

