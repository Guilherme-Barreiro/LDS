using System.Threading.Tasks;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IPacienteRepository : IGenericRepository<Paciente>
    {
        Task<Paciente?> GetByNUtenteAsync(string nUtente);
    }
}
