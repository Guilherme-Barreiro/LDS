using ConsultaPlus.Core.Models;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IAdminRepository
    {
        Task<Admin?> GetByUsernameAsync(string username);
        Task<Admin?> GetByEmailAsync(string email);
        Task UpdateAsync(Admin admin);
    }
}