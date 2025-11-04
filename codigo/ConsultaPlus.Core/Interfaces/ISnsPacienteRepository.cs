using ConsultaPlus.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface ISnsPacienteRepository : IGenericRepository<SnsPaciente>
    {
        Task<IEnumerable<SnsPaciente>> SearchByNomeAsync(string termo);
        Task<SnsPaciente?> GetByEmailAsync(string email);
    }
}
