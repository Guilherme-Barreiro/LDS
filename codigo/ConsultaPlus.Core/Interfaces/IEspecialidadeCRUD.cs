using ConsultaPlus.Core.Models;

public interface IEspecialidadeCRUD

{
    Task<IEnumerable<Especialidade>> GetAllAsync();
    Task<Especialidade?> GetByIdAsync(int id);
    Task<IEnumerable<Especialidade>> GetByNameAsync(string name);
    Task<int> AddAsync(string name);
    Task UpdateAsync(int id, string newNome);
    Task DeleteAsync(int id);
}
