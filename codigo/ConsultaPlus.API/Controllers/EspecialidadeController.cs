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
        private readonly IEspecialidadeCRUD _especialidadeCRUD;

        public EspecialidadeController(IEspecialidadeCRUD especialidadeCRUD)
        {
            _especialidadeCRUD = especialidadeCRUD;
        }

        [HttpPost("registo-especialidade")]
        public async Task<IActionResult> Create([FromBody] CreateEspecialidadeDTO requestDto)
        {
            try
            {
                var id = await _especialidadeCRUD.AddAsync(requestDto.Nome);

                var readDto = new ReadEspecialidadeDTO
                {
                    Id = id,
                    Nome = requestDto.Nome.Trim()
                };

                return CreatedAtAction(nameof(GetByName), new { id = id }, readDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Não foi possível registar a especialidade devido a um conflito na base de dados." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("remover-especialidade/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _especialidadeCRUD.DeleteAsync(id);
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Não foi possível remover a especialidade devido a um conflito na base de dados." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("atualizar-especialidade/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEspecialidadeDTO requestDto)
        {
            try
            {
                var novoNome = requestDto.Nome.Trim();
                await _especialidadeCRUD.UpdateAsync(id, novoNome);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Não foi possível atualizar a especialidade devido a um conflito na base de dados." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("obter-especialidade-id/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var ent = await _especialidadeCRUD.GetByIdAsync(id);
                if (ent == null) return NotFound(new { message = "Especialidade não encontrada." });

                return Ok(new ReadEspecialidadeDTO { Id = ent.Id, Nome = ent.Nome });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("obter-especialidade-nome/{string}")]
        public async Task<IActionResult> GetByName(string nome)
        {
            try
            {
                var todas = await _especialidadeCRUD.GetByNameAsync(nome);
                if (todas == null) return NotFound(new { message = "Especialidades não encontradas." });

                var dtos = todas.Select(e => new ReadEspecialidadeDTO { Id = e.Id, Nome = e.Nome });
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("obter-todas-especialidades")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var todas = await _especialidadeCRUD.GetAllAsync();
                var dtos = todas.Select(e => new ReadEspecialidadeDTO { Id = e.Id, Nome = e.Nome });
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}