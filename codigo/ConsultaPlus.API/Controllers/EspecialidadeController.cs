using ConsultaPlus.API.DTOs;
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
    }
    public async Task<IActionResult> Add(EspecialidadeDto requestDto)
        {
            try
            {
                var novaEspecialidade = new Especialidade
                {
                    Nome = requestDto.Nome
                };
                
                await _especialidadeCRUD.Add(novaEspecialidade);
                
                return StatusCode(201, "Paciente registado com sucesso.");
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

}