using ConsultaPlus.Core.Models;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IPacienteRepository
    {
        Task<IEnumerable<Paciente>> GetAllAsync();
        Task<Paciente?> GetByIdAsync(int id);
        Task<Paciente?> GetByNUtenteAsync(string nUtente);

        Task AddAsync(Paciente paciente);
        Task UpdateAsync(Paciente paciente);
        Task DeleteAsync(int id);
    }
}
