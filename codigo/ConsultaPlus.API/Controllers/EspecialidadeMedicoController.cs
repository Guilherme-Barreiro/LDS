using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeMedicoController : ControllerBase
    {
        private readonly IEspecialidadeMedicoService _especialidadeMedicoService;

        public EspecialidadeMedicoController(IEspecialidadeMedicoService especialidadeMedicoService)
        {
            _especialidadeMedicoService = especialidadeMedicoService;
        }


        [HttpPost("associar-especialidade-medico")]
        public async Task<IActionResult> AddEspecialidadeMedico([FromBody] EspecialidadeMedicoDTO requestDto)
        {
            try
            {
                await _especialidadeMedicoService.AddAsync(requestDto.MedicoId, requestDto.EspecialidadeId);
                return StatusCode(201, new { message = "Especialidade associada ao médico com sucesso." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Erro de base de dados ao associar especialidade ao médico." });
            }
        }

        [HttpDelete("remover-especialidade-medico")]
        public async Task<IActionResult> DeleteEspecialidadeMedico([FromBody] EspecialidadeMedicoDTO requestDto)
        {
            try
            {
                await _especialidadeMedicoService.DeleteAsync(requestDto.MedicoId, requestDto.EspecialidadeId);
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

            var medicos = await _especialidadeMedicoService.GetMedicosByEspecialidadeIdAsync(especialidadeId);
            return Ok(medicos);

        }

        [HttpGet("especialidades-por-medico/{medicoId}")]
        public async Task<IActionResult> GetEspecialidadesByMedicoId(int medicoId)
        {

            var especialidades = await _especialidadeMedicoService.GetEspecialidadesByMedicoIdAsync(medicoId);
            return Ok(especialidades);

        }
    }
}