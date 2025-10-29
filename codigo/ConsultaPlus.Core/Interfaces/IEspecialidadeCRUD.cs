using ConsultaPlus.Core.Models;

public interface IEspecialidadeCRUD

{
    Task<IEnumerable<Especialidade>> GetAllAsync();
    Task<Especialidade?> GetByIdAsync(int id);
    Task AddAsync(Especialidade especialidade);
    Task UpdateAsync(Especialidade especialidade, string newNome);
    Task DeleteAsync(int id);
}
