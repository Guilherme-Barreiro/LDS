using Azure.Core;
using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;

        public RelatorioController(IRelatorioService relatorioService)
        {
            _relatorioService = relatorioService;
        }

        [HttpGet("consultas-por-periodo")]
        public async Task<IActionResult> GetConsultasPorPeriodo([FromQuery] ConsultasPorPeriodoRequestDTO requestDTO)
        {
            try
            {
                var result = await _relatorioService.GetConsultasPorPeriodoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId);
                return Ok(result);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possivel atualizar a especialidade devido a um conflito na base de dados." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Erro ao gerar relatório." });
            }
        }

        [HttpGet("taxa-nao-comparecimento")]
        public async Task<IActionResult> GetTaxaNaoComparecimento([FromQuery] TaxaNaoComparecimentoRequestDTO requestDTO)
        {
            try
            {
                var result = await _relatorioService.GetTaxaNaoComparecimentoAsync(requestDTO.DataInicio, requestDTO.DataFim, requestDTO.MedicoId, requestDTO.EspecialidadeId);
                return Ok(result);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possivel atualizar a especialidade devido a um conflito na base de dados." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Erro ao gerar relatorio de nao comparecimento." });
            }
        }
    }
}
