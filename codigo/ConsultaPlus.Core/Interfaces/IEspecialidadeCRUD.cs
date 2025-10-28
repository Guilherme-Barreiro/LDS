using ConsultaPlus.Core.Models;

public interface IEspecialidadeCRUD

{
    Task<IEnumerable<Especialidade>> GetAll();
    Task<Especialidade?> GetById(int id);
    Task Add(Especialidade especialidade);
    Task Update(Especialidade especialidade);
    Task Delete(int id);
}
