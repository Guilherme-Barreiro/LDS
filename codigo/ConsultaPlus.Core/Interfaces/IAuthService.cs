using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(string nUtente, string password);
        Task LogoutAsync(string jwt);
        Task<string> CreatePasswordResetAsync(string identifier);
        Task ResetPasswordAsync(string token, string newPassword);
    }
}
