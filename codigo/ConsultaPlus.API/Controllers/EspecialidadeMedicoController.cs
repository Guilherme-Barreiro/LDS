using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeMedicoController : ControllerBase
    {
        private readonly IEspecialidadeMedico _especialidadeMedico;

        public EspecialidadeMedicoController(IEspecialidadeMedico especialidadeMedico)
        {
            _especialidadeMedico = especialidadeMedico;
        }


        [HttpPost("associar-especialidade-medico")]
        public async Task<IActionResult> AssociarEspecialidadeMedico([FromBody] EspecialidadeMedicoDTO requestDto)
        {
            try
            {

                if (!await _especialidadeMedico.MedicoExistsAsync(requestDto.MedicoId))
                    return NotFound(new { message = "Médico não encontrado." });

                if (!await _especialidadeMedico.EspecialidadeExistsAsync(requestDto.EspecialidadeId))
                    return NotFound(new { message = "Especialidade não encontrada." });

                if (await _especialidadeMedico.ExistsAsync(requestDto.MedicoId, requestDto.EspecialidadeId))
                    return Conflict(new { message = "O médico já possui essa especialidade associada." });


                var associacao = new EspecialidadeMedico
                {
                    MedicoId = requestDto.MedicoId,
                    EspecialidadeId = requestDto.EspecialidadeId
                };
                await _especialidadeMedico.AddAsync(associacao);
                return StatusCode(201, "Especialidade associada ao médico com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("remover-especialidade-medico")]
        public async Task<IActionResult> RemoverEspecialidadeMedico([FromBody] EspecialidadeMedicoDTO requestDto)
        {
            try
            {
                await _especialidadeMedico.RemoveAsync(requestDto.MedicoId, requestDto.EspecialidadeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("medicos-por-especialidade/{especialidadeId}")]
        public async Task<IActionResult> GetMedicosByEspecialidadeId(int especialidadeId)
        {
            try
            {
                var medicos = await _especialidadeMedico.GetMedicosByEspecialidadeIdAsync(especialidadeId);
                return Ok(medicos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("especialidades-por-medico/{medicoId}")]
        public async Task<IActionResult> GetEspecialidadesByMedicoId(int medicoId)
        {
            try
            {
                var especialidades = await _especialidadeMedico.GetEspecialidadesByMedicoIdAsync(medicoId);
                return Ok(especialidades);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}