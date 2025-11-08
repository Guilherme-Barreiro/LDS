using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IMedicoService
    {
        Task<IEnumerable<Medico>> GetAllAsync(CancellationToken ct = default);
        Task<Medico?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<Medico>> SearchByNomeAsync(string nome, CancellationToken ct = default);
        Task<Medico> CreateAsync(Medico novo, CancellationToken ct = default);
        Task UpdateAsync(Medico medico, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}