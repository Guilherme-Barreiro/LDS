using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IEspecialidadeMedico
    {
        Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId);
        Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId);

        Task AddAsync(EspecialidadeMedico especialidadeMedico);
        Task RemoveAsync(int medicoId, int especialidadeId);

        Task<bool> ExistsAsync(int medicoId, int especialidadeId);
        Task<bool> EspecialidadeExistsAsync(int especialidadeId);
        Task<bool> MedicoExistsAsync(int medicoId);
        
    }
}
