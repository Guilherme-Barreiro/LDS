using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class MedicoRepository : IMedicoRepository
    {
        private readonly ApplicationDbContext _context;

        public MedicoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Medico?> GetByNUtenteAsync(string nUtente)
        {
            return await _context.Medicos.FirstOrDefaultAsync(m => m.NUtente == nUtente);
        }

        // --- INÍCIO DO NOVO CÓDIGO ---

        public async Task<Medico?> GetByEmailAsync(string email)
        {
            // Procura na tabela Medicos pelo email
            return await _context.Medicos.FirstOrDefaultAsync(m => m.Email == email);
        }

        public async Task UpdateAsync(Medico medico)
        {
            // Diz ao Entity Framework para seguir as alterações nesta entidade
            _context.Medicos.Update(medico);
            // Guarda todas as alterações pendentes na base de dados
            await _context.SaveChangesAsync();
        }

        // --- FIM DO NOVO CÓDIGO ---
    }
}