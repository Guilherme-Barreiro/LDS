using ConsultaPlus.API.DTOs;
using ConsultaPlus.API.Helpers;
using ConsultaPlus.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/admin/medicos/{medicoId:int}")]
    //[Authorize(Roles = "Admin")]
    public class AdminHorariosController : ControllerBase
    {
        private readonly IHorarioTrabalhoMedico _horarioSvc;
        private readonly IHorarioExcecaoMedico _excecaoSvc;

        public AdminHorariosController(IHorarioTrabalhoMedico horarioSvc, IHorarioExcecaoMedico excecaoSvc)
        {
            _horarioSvc = horarioSvc;
            _excecaoSvc = excecaoSvc;
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
    }
}
