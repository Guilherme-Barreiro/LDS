using ConsultaPlus.API.DTOs;
using ConsultaPlus.API.Helpers;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/admin/medicos/{medicoId:int}")]
    //[Authorize(Roles = "Admin")]
    public class AdminHorariosController : ControllerBase
    {
        private readonly IHorarioTrabalhoMedico _horarioSvc;
        private readonly IHorarioExcecaoMedico _excecaoSvc;
        private readonly ApplicationDbContext _db;

        public AdminHorariosController(IHorarioTrabalhoMedico horarioSvc, IHorarioExcecaoMedico excecaoSvc, ApplicationDbContext db)
        {
            _horarioSvc = horarioSvc;
            _excecaoSvc = excecaoSvc;
            _db = db;
        }

        [HttpPost("horario")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DefinirHorario(
            int medicoId,
            [FromBody] DefinirHorarioRequest req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var diaNormalizado = DiaSemanaHelper.Normalizar(req.DiaSemana); 

            try
            {
                await _horarioSvc.DefinirHorarioAsync(
                    medicoId, diaNormalizado, req.HoraInicio, req.HoraFim, ct);
                return NoContent();
            }
            catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
        }

        [HttpPost("excecoes")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RegistarExcecao(
            int medicoId,
            [FromBody] RegistarExcecaoRequest req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _excecaoSvc.RegistarExcecaoAsync(
                    medicoId, req.Data, req.HoraInicio, req.HoraFim, req.IsReducao, req.Motivo, ct);
                return NoContent();
            }
            catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
        }
        // GET lista de horários do médico
        [HttpGet("horario")]
        public async Task<IActionResult> GetHorarios(int medicoId, CancellationToken ct)
        {
            var lista = await _db.HorariosTrabalhoMedicos
                .Where(h => h.MedicoId == medicoId)
                .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
                .Select(h => new { h.Id, h.MedicoId, h.DiaSemana, h.HoraInicio, h.HoraFim })
                .ToListAsync(ct);

            return Ok(lista);
        }

        // GET um horário específico
        [HttpGet("horario/{horarioId:int}")]
        public async Task<IActionResult> GetHorario(int medicoId, int horarioId, CancellationToken ct)
        {
            var h = await _db.HorariosTrabalhoMedicos
                .Where(x => x.MedicoId == medicoId && x.Id == horarioId)
                .Select(x => new { x.Id, x.MedicoId, x.DiaSemana, x.HoraInicio, x.HoraFim })
                .FirstOrDefaultAsync(ct);

            return h is null ? NotFound(new { error = "Horário não encontrado." }) : Ok(h);
        }

        // PUT atualizar um horário (usa a normalização e as mesmas validações do service)
        [HttpPut("horario/{horarioId:int}")]
        public async Task<IActionResult> AtualizarHorario(
            int medicoId, int horarioId, [FromBody] AtualizarHorarioRequest req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var dia = DiaSemanaHelper.Normalizar(req.DiaSemana);

            try
            {
                await _horarioSvc.AtualizarHorarioAsync(medicoId, horarioId, dia, req.HoraInicio, req.HoraFim, ct);

                var atualizado = await _db.HorariosTrabalhoMedicos
                    .Where(h => h.Id == horarioId)
                    .Select(h => new { h.Id, h.MedicoId, h.DiaSemana, h.HoraInicio, h.HoraFim })
                    .FirstAsync(ct);

                return Ok(atualizado);
            }
            catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
            catch (UnauthorizedAccessException e) { return Forbid(e.Message); }
            catch (ArgumentException e) { return BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Conflict(new { error = e.Message }); }
        }
        // GET lista de exceções de horario do médico
        [HttpGet("excecoes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExcecoes(
            int medicoId,
            [FromQuery] DateOnly? data,
            CancellationToken ct)
        {
            var query = _db.HorariosExcecaoMedicos
                .Where(e => e.MedicoId == medicoId);

            if (data.HasValue)
            {
                var d = data.Value.ToDateTime(TimeOnly.MinValue).Date;
                query = query.Where(e => e.Data == d);
            }

            var lista = await query
                .OrderBy(e => e.Data).ThenBy(e => e.HoraInicio)
                .Select(e => new ExcecaoDto
                {
                    Id = e.Id,
                    MedicoId = e.MedicoId,
                    Data = DateOnly.FromDateTime(e.Data),
                    HoraInicio = e.HoraInicio,
                    HoraFim = e.HoraFim,
                    IsReducao = e.IsReducao,
                    Motivo = e.Motivo
                })
                .ToListAsync(ct);

            return Ok(lista);
        }

        [HttpDelete("horario/{horarioId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverHorario(int medicoId, int horarioId, CancellationToken ct)
        {
            var h = await _db.HorariosTrabalhoMedicos.FirstOrDefaultAsync(x => x.MedicoId == medicoId && x.Id == horarioId, ct);
            if (h is null) return NotFound(new { error = "Horário não encontrado." });

            _db.HorariosTrabalhoMedicos.Remove(h);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

    }
}
