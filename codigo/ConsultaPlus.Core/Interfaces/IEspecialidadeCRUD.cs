using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IEspecialidadeCRUD
    {
        Task<IEnumerable<Especialidade>> GetAllAsync();
        Task<Especialidade?> GetByIdAsync(int id);
        Task AddAsync(Especialidade especialidade);
        Task UpdateAsync(Especialidade especialidade);
        Task DeleteAsync(int id);

        Task<bool> ExistsByNameAsync(string nome);
        Task<bool> HasMedicosAsync(int especialidadeId);
    }
}
