using ConsultaPlus.API.DTOs.Especialidade;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadeController : ControllerBase
    {
        private readonly IEspecialidadeService _especialidadeService;

        public EspecialidadeController(IEspecialidadeService especialidadeService)
        {
            _especialidadeService = especialidadeService;
        }

        [HttpPost("registo-especialidade")]
        public async Task<IActionResult> Create([FromBody] CreateEspecialidadeDTO requestDto)
        {
            try
            {
                var id = await _especialidadeService.AddAsync(requestDto.Nome);

                var readDto = new ReadEspecialidadeDTO
                {
                    Id = id,
                    Nome = requestDto.Nome.Trim()
                };

                return CreatedAtAction(nameof(Search), new { id = id }, readDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possivel registar a especialidade devido a um conflito na base de dados." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("remover-especialidade/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _especialidadeService.DeleteAsync(id);
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possivel remover a especialidade devido a um conflito na base de dados." });
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

        [HttpPut("atualizar-especialidade/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEspecialidadeDTO requestDto)
        {
            try
            {
                var novoNome = requestDto.Nome.Trim();
                await _especialidadeService.UpdateAsync(id, novoNome);
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Nao foi possivel atualizar a especialidade devido a um conflito na base de dados." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("obter-especialidade-id/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ent = await _especialidadeService.GetByIdAsync(id);
            if (ent == null) return NotFound(new { message = "Especialidade nao encontrada." });

            return Ok(new ReadEspecialidadeDTO { Id = ent.Id, Nome = ent.Nome });
            
        }

        [HttpGet("pesquisar-especialidade")]
        public async Task<IActionResult> Search([FromQuery] string termo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termo))
                {
                    return BadRequest(new { message = "Termo de pesquisa e obrigatorio." });
                }

                var resultados = await _especialidadeService.SearchAsync(termo);
                if (!resultados.Any())
                    return NotFound(new { message = "Nenhuma especialidade encontrada." });

                var dtos = resultados.Select(e => new ReadEspecialidadeDTO { Id = e.Id, Nome = e.Nome });
                return Ok(dtos);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }


        [HttpGet("obter-todas-especialidades")]
        public async Task<IActionResult> GetAll()
        {
            var todas = await _especialidadeService.GetAllAsync();
            var dtos = todas.Select(e => new ReadEspecialidadeDTO { Id = e.Id, Nome = e.Nome });
            return Ok(dtos);
        }

    }
}