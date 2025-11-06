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

        public AdminHorariosController(
            IHorarioTrabalhoMedico horarioSvc,
            IHorarioExcecaoMedico excecaoSvc,
            ApplicationDbContext db)
        {
            _horarioSvc = horarioSvc;
            _excecaoSvc = excecaoSvc;
            _db = db;
        }

        // POST /api/admin/medicos/{medicoId}/horario
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var diaNormalizado = DiaSemanaHelper.Normalizar(req.DiaSemana);

            try
            {
                await _horarioSvc.DefinirHorarioAsync(
                    medicoId,
                    diaNormalizado,
                    req.HoraInicio,
                    req.HoraFim,
                    ct
                );

                return NoContent();
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { error = e.Message });
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { error = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return Conflict(new { error = e.Message });
            }
        }

        // POST /api/admin/medicos/{medicoId}/excecoes
        [HttpPost("excecoes")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RegistarExcecao(
            int medicoId,
            [FromBody] RegistarExcecaoRequest req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _excecaoSvc.RegistarExcecaoAsync(
                    medicoId,
                    req.Data,
                    req.HoraInicio,
                    req.HoraFim,
                    req.IsReducao,
                    req.Motivo,
                    ct
                );

                return NoContent();
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { error = e.Message });
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        // GET /api/admin/medicos/{medicoId}/horario
        [HttpGet("horario")]
        public async Task<IActionResult> GetHorarios(int medicoId, CancellationToken ct)
        {
            var lista = await _db.HorariosTrabalhoMedicos
                .Where(h => h.MedicoId == medicoId)
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraInicio)
                .Select(h => new
                {
                    h.Id,
                    h.MedicoId,
                    h.DiaSemana,
                    h.HoraInicio,
                    h.HoraFim
                })
                .ToListAsync(ct);

            return Ok(lista);
        }

        // GET /api/admin/medicos/{medicoId}/horario/{horarioId}
        [HttpGet("horario/{horarioId:int}")]
        public async Task<IActionResult> GetHorario(int medicoId, int horarioId, CancellationToken ct)
        {
            var h = await _db.HorariosTrabalhoMedicos
                .Where(x => x.MedicoId == medicoId && x.Id == horarioId)
                .Select(x => new
                {
                    x.Id,
                    x.MedicoId,
                    x.DiaSemana,
                    x.HoraInicio,
                    x.HoraFim
                })
                .FirstOrDefaultAsync(ct);

            return h is null
                ? NotFound(new { error = "Horário não encontrado." })
                : Ok(h);
        }

        // PUT /api/admin/medicos/{medicoId}/horario/{horarioId}
        [HttpPut("horario/{horarioId:int}")]
        public async Task<IActionResult> AtualizarHorario(
            int medicoId,
            int horarioId,
            [FromBody] AtualizarHorarioRequest req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dia = DiaSemanaHelper.Normalizar(req.DiaSemana);

            try
            {
                await _horarioSvc.AtualizarHorarioAsync(
                    medicoId,
                    horarioId,
                    dia,
                    req.HoraInicio,
                    req.HoraFim,
                    ct
                );

                var atualizado = await _db.HorariosTrabalhoMedicos
                    .Where(h => h.Id == horarioId)
                    .Select(h => new
                    {
                        h.Id,
                        h.MedicoId,
                        h.DiaSemana,
                        h.HoraInicio,
                        h.HoraFim
                    })
                    .FirstAsync(ct);

                return Ok(atualizado);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { error = e.Message });
            }
            catch (UnauthorizedAccessException e)
            {
                return Forbid(e.Message);
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { error = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return Conflict(new { error = e.Message });
            }
        }

        // GET /api/admin/medicos/{medicoId}/excecoes
        [HttpGet("excecoes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExcecoes(
            int medicoId,
            CancellationToken ct)
        {
            var lista = await _db.HorariosExcecaoMedicos
                .Where(e => e.MedicoId == medicoId)
                .OrderBy(e => e.Data)
                .ThenBy(e => e.HoraInicio)
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


        // DELETE /api/admin/medicos/{medicoId}/horario/{horarioId}
        [HttpDelete("horario/{horarioId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverHorario(int medicoId, int horarioId, CancellationToken ct)
        {
            var h = await _db.HorariosTrabalhoMedicos
                .FirstOrDefaultAsync(x => x.MedicoId == medicoId && x.Id == horarioId, ct);

            if (h is null)
                return NotFound(new { error = "Horário não encontrado." });

            _db.HorariosTrabalhoMedicos.Remove(h);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }

        // GET /api/admin/medicos/{medicoId}/excecoes/{horarioId}
        [HttpGet("excecoes/{horarioId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetExcecao(int medicoId, int horarioId, CancellationToken ct)
        {
            var e = await _db.HorariosExcecaoMedicos
                .Where(x => x.MedicoId == medicoId && x.Id == horarioId)
                .Select(x => new ExcecaoDto
                {
                    Id = x.Id,
                    MedicoId = x.MedicoId,
                    Data = DateOnly.FromDateTime(x.Data),
                    HoraInicio = x.HoraInicio,
                    HoraFim = x.HoraFim,
                    IsReducao = x.IsReducao,
                    Motivo = x.Motivo
                })
                .FirstOrDefaultAsync(ct);

            return e is null
                ? NotFound(new { error = "Exceção não encontrada." })
                : Ok(e);
        }

        // PUT /api/admin/medicos/{medicoId}/excecoes/{horarioId}
        [HttpPut("excecoes/{horarioId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarExcecao(
            int medicoId,
            int horarioId,
            [FromBody] AtualizarExcecaoRequest req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (req.HoraInicio >= req.HoraFim)
                return BadRequest(new { error = "HoraInicio deve ser anterior a HoraFim." });

            var e = await _db.HorariosExcecaoMedicos
                .FirstOrDefaultAsync(x => x.MedicoId == medicoId && x.Id == horarioId, ct);

            if (e is null)
                return NotFound(new { error = "Exceção não encontrada." });

            e.Data = req.Data.ToDateTime(TimeOnly.MinValue);
            e.HoraInicio = req.HoraInicio;
            e.HoraFim = req.HoraFim;
            e.IsReducao = req.IsReducao;
            e.Motivo = req.Motivo;

            await _db.SaveChangesAsync(ct);

            var dto = new ExcecaoDto
            {
                Id = e.Id,
                MedicoId = e.MedicoId,
                Data = DateOnly.FromDateTime(e.Data),
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                IsReducao = e.IsReducao,
                Motivo = e.Motivo
            };

            return Ok(dto);
        }

        // DELETE /api/admin/medicos/{medicoId}/excecoes/{horarioId}
        [HttpDelete("excecoes/{horarioId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverExcecao(int medicoId, int horarioId, CancellationToken ct)
        {
            var e = await _db.HorariosExcecaoMedicos
                .FirstOrDefaultAsync(x => x.MedicoId == medicoId && x.Id == horarioId, ct);

            if (e is null)
                return NotFound(new { error = "Exceção não encontrada." });

            _db.HorariosExcecaoMedicos.Remove(e);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}
