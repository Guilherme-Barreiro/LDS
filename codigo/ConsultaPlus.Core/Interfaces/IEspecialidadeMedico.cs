using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IEspecialidadeMedicoCRUD
    {
        Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId);
        Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId);

        Task AddAsync(EspecialidadeMedico especialidadeMedico);
        Task RemoveAsync(int medicoId, int especialidadeId);
    }
}
