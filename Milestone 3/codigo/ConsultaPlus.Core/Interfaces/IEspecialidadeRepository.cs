using ConsultaPlus.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IEspecialidadeRepository : IGenericRepository<Especialidade>
    {
        Task<IEnumerable<Especialidade>> SearchByNameAsync(string nome);
        Task<bool> IsLinkedToMedic(int especialidadeId);
        Task<bool> ExistsByNameAsync(string nome);
        Task<bool> ExistsByNameAndNotIdAsync(string nome, int id);
    }
}
