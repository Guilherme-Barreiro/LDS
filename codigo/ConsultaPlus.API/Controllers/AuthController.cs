using ConsultaPlus.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// NOTA: Vamos precisar de um DTO para receber os dados
public class RegisterPacienteRequest
{
    public string NomeCompleto { get; set; }
    public string NUtente { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registo-paciente")]
    public async Task<IActionResult> Register(RegisterPacienteRequest request)
    {
        try
        {
            await _authService.RegisterPacienteAsync(request.NomeCompleto, request.NUtente, request.Password, request.Email);
            return StatusCode(201); // 201 Created
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message); // Retorna uma mensagem de erro
        }
    }
}