using ConsultaPlus.Core.Models;

public interface IEspecialidadeService

{
    Task<IEnumerable<Especialidade>> GetAllAsync();
    Task<Especialidade?> GetByIdAsync(int id);
    Task<IEnumerable<Especialidade>> SearchAsync(string termo);
    Task<int> AddAsync(string name);
    Task UpdateAsync(int id, string newNome);
    Task DeleteAsync(int id);
}
