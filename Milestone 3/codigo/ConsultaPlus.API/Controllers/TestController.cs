using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; 

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("publico")]
        public IActionResult GetDadosPublicos()
        {
            return Ok("Isto é uma informação pública que todos podem ver.");
        }

        [HttpGet("protegido")]
        [Authorize] 
        public IActionResult GetDadosProtegidos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userRole = User.FindFirstValue(ClaimTypes.Role); 

            return Ok($"Olá, utilizador com ID '{userId}', Email '{userEmail}' e Papel '{userRole}'. Você está autenticado e pode ver esta informação secreta.");
        }
    }
}
