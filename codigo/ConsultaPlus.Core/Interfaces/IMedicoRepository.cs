using ConsultaPlus.Core.Models;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IMedicoRepository
    {
        Task<Medico?> GetByNUtenteAsync(string nUtente);

        // --- ADICIONE ESTAS DUAS LINHAS ---
        Task<Medico?> GetByEmailAsync(string email);
        Task UpdateAsync(Medico medico);
        // ---------------------------------
    }
}