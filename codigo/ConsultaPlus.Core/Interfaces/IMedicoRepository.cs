using ConsultaPlus.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IMedicoRepository : IGenericRepository<Medico>
    {
        Task<Medico?> GetByEmailAsync(string email);
        Task<IEnumerable<Medico>> SearchByNameAsync(string nome);
        Task<bool> ExistsAsync(int medicoId);
    }
}
