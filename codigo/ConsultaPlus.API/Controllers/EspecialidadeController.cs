using ConsultaPlus.API.DTOs.Especialidade;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> RegistarEspecialidade([FromBody] CreateEspecialidadeDTO requestDto)
        {
            try
            {
                var existente = (await _especialidadeCRUD.GetAllAsync())
                    .FirstOrDefault(e => e.Nome.ToLower() == requestDto.Nome.ToLower());

                if (existente != null)
                    return Conflict(new { message = "Já existe uma especialidade com esse nome." });

                var novaEspecialidade = new Especialidade { Nome = requestDto.Nome };
                await _especialidadeCRUD.AddAsync(novaEspecialidade);

                return CreatedAtAction(nameof(ObterEspecialidade),
                    new { id = novaEspecialidade.Id },
                    new ReadEspecialidadeDTO { Id = novaEspecialidade.Id, Nome = novaEspecialidade.Nome });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("remover-especialidade/{id}")]
        public async Task<IActionResult> RemoverEspecialidade(int id)
        {
            try
            {
                var especialidade = await _especialidadeCRUD.GetByIdAsync(id);
                if (especialidade == null)
                {
                    return NotFound(new { message = "Especialidade não encontrada." });
                }
                await _especialidadeCRUD.DeleteAsync(id);
                return StatusCode(201, "Especialidade removida com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("atualizar-especialidade/{id}")]
        public async Task<IActionResult> AtualizarEspecialidade(int id, [FromBody] ReadEspecialidadeDTO requestDto)
        {
            try
            {
                var especialidadeExistente = await _especialidadeCRUD.GetByIdAsync(id);
                if (especialidadeExistente == null)
                {
                    return NotFound(new { message = "Especialidade não encontrada." });
                }

                var todasEspecialidades = await _especialidadeCRUD.GetAllAsync();
                var nomeDuplicado = todasEspecialidades
                    .Any(e => e.Nome.ToLower() == requestDto.Nome.ToLower() && e.Id != id);

                if (nomeDuplicado)
                {
                    return Conflict(new { message = "Já existe outra especialidade com esse nome." });
                }

                especialidadeExistente.Nome = requestDto.Nome;
                await _especialidadeCRUD.UpdateAsync(especialidadeExistente);
                return StatusCode(201, "Especialidade atualizada com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("obter-especialidade/{id}")]
        public async Task<IActionResult> ObterEspecialidade(int id)
        {
            try
            {
                var especialidade = await _especialidadeCRUD.GetByIdAsync(id);
                if (especialidade == null)
                {
                    return NotFound(new { message = "Especialidade não encontrada." });
                }
                var especialidadeDto = new
                {
                    Nome = especialidade.Nome
                };

                return StatusCode(201, especialidadeDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpGet("obter-todas-especialidades")]
        public async Task<IActionResult> ObterEspecialidades()
        {
            try
            {
                var especialidades = await _especialidadeCRUD.GetAllAsync();
                var especialidadesDto = especialidades
                .Select(e => new { Nome = e.Nome })
                .ToList();
                return StatusCode(201, especialidadesDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}