using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; 

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // Endpoint público que qualquer pessoa pode aceder
        [HttpGet("publico")]
        public IActionResult GetDadosPublicos()
        {
            return Ok("Isto é uma informação pública que todos podem ver.");
        }

        // Endpoint protegido que SÓ utilizadores autenticados podem aceder
        [HttpGet("protegido")]
        [Authorize] // <-- A magia acontece aqui
        public IActionResult GetDadosProtegidos()
        {
            // Depois de autenticado, podemos aceder aos dados do utilizador que estão no token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Encontra o 'sub' (ID do utilizador)
            var userEmail = User.FindFirstValue(ClaimTypes.Email); // Encontra o 'email'
            var userRole = User.FindFirstValue(ClaimTypes.Role); // Encontra o 'role'

            return Ok($"Olá, utilizador com ID '{userId}', Email '{userEmail}' e Papel '{userRole}'. Você está autenticado e pode ver esta informação secreta.");
        }
    }
}
