using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IEspecialidadeMedicoService
    {
        Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId);
        Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId);

        Task AddAsync(int medicoId, int especialidadeId);
        Task DeleteAsync(int medicoId, int especialidadeId);
        
    }
}
