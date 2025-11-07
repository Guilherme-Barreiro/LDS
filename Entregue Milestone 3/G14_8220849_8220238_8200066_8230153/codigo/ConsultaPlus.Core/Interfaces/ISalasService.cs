using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface ISalasService
    {
        Task<IEnumerable<Sala>> GetAllAsync();
        Task<Sala?> GetByIdAsync(int id);
        Task<IEnumerable<Sala>> SearchAsync(string nome);
        Task<int> CreateAsync(string nome);
        Task DeleteAsync(int id);
    }
}
