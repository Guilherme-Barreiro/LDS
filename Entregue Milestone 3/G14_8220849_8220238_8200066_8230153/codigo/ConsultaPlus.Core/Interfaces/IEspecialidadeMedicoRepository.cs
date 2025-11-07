using ConsultaPlus.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IEspecialidadeMedicoRepository : IGenericRepository<EspecialidadeMedico>
    {
        Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId);
        Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId);
        Task<bool> ExistsAsync(int medicoId, int especialidadeId);
        Task<bool> EspecialidadeExistsAsync(int especialidadeId);
        Task<bool> MedicoExistsAsync(int medicoId);
        Task DeleteAsync(int medicoId, int especialidadeId);
    }
}
