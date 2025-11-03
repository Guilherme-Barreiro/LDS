using ConsultaPlus.Core.Models;
using System.Threading.Tasks;
namespace ConsultaPlus.Core.Interfaces
{
    public interface IAuthService
    {
        Task RegisterPacienteAsync(Paciente novoPaciente, string password);
        Task<string> LoginAsync(string nUtente, string password);
        Task ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
 