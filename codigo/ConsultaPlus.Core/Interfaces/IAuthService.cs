using ConsultaPlus.Core.Models;
namespace ConsultaPlus.Core.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(string nUtente, string password);
        
    }
}
 