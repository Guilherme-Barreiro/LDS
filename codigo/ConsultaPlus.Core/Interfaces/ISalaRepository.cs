using System;                           
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface ISalaRepository : IGenericRepository<Sala>
    {
        Task<IEnumerable<Sala>> SearchByNameAsync(string nome);
        Task<bool> ExistsByNameAsync(string nome);
        Task<bool> HasFutureConsultasAsync(int salaId, DateTime now);
    }
}
