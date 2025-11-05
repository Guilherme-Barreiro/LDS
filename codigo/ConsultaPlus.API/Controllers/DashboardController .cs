using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.API.DTOs.Consultas;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IConsultaRepository _repo;
        public DashboardController(IConsultaRepository repo) => _repo = repo;

        [HttpGet("medico/{medicoId:int}/consultas")]
        public async Task<IActionResult> GetAgendaMedico(
            int medicoId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] bool onlyConfirmed = false,
            CancellationToken ct = default)
        {
            var start = (from ?? DateTime.UtcNow.Date).Date;
            var endExclusive = ((to ?? start.AddDays(28)).Date).AddDays(1);

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
        public async Task<IActionResult> GetHistoricoPaciente(
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
    }
}
