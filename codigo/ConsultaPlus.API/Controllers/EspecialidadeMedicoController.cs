using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeMedicoController : ControllerBase
    {
        private readonly IEspecialidadeMedicoService _especialidadeMedico;

        public EspecialidadeMedicoController(IEspecialidadeMedicoService especialidadeMedico)
        {
            _especialidadeMedico = especialidadeMedico;
        }


        [HttpPost("associar-especialidade-medico")]
        public async Task<IActionResult> AddEspecialidadeMedico([FromBody] EspecialidadeMedicoDTO requestDto)
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
                await _especialidadeMedico.AddAsync(requestDto.MedicoId, requestDto.EspecialidadeId);
                return StatusCode(201, "Especialidade associada ao médico com sucesso.");
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possível remover a especialidade devido a um conflito na base de dados." });
            }
        }

        [HttpDelete("remover-especialidade-medico")]
        public async Task<IActionResult> DeleteEspecialidadeMedico([FromBody] EspecialidadeMedicoDTO requestDto)
        {
            try
            {
                await _especialidadeMedico.DeleteAsync(requestDto.MedicoId, requestDto.EspecialidadeId);
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possível remover a especialidade devido a um conflito na base de dados." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("medicos-por-especialidade/{especialidadeId}")]
        public async Task<IActionResult> GetMedicosByEspecialidadeId(int especialidadeId)
        {

            var medicos = await _especialidadeMedico.GetMedicosByEspecialidadeIdAsync(especialidadeId);
            return Ok(medicos);

        }

        [HttpGet("especialidades-por-medico/{medicoId}")]
        public async Task<IActionResult> GetEspecialidadesByMedicoId(int medicoId)
        {

            var especialidades = await _especialidadeMedico.GetEspecialidadesByMedicoIdAsync(medicoId);
            return Ok(especialidades);

        }
    }
}