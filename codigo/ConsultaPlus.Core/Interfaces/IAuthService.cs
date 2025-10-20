using System.Threading.Tasks;


namespace ConsultaPlus.Core.Interfaces;

public interface IAuthService
{
    Task RegisterPacienteAsync(string nomeCompleto, string nUtente, string password, string email /*...outros campos*/);
}