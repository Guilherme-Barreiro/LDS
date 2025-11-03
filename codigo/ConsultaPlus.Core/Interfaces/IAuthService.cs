using ConsultaPlus.Core.Models;
namespace ConsultaPlus.Core.Interfaces
{
    public interface IAuthService
    {
        Task RegisterPacienteAsync(Paciente novoPaciente, string password);
        Task<string> LoginAsync(string nUtente, string password);
    }
}
 