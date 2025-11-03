using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class MedicoRepository : GenericRepository<Medico>, IMedicoRepository
    {
        public MedicoRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Medico?> GetByEmailAsync(string email) =>
            await _context.Medicos.AsNoTracking().FirstOrDefaultAsync(m => m.Email == email);

        public async Task<Medico?> GetByNUtenteAsync(string nUtente) =>
            await _context.Medicos.AsNoTracking().FirstOrDefaultAsync(m => m.NUtente == nUtente);

        public async Task<IEnumerable<Medico>> SearchByNameAsync(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return Enumerable.Empty<Medico>();

            var term = $"%{nome.Trim()}%";
            return await _context.Medicos
                .AsNoTracking()
                .Where(m =>
                    EF.Functions.Like(m.NomeCompleto, term) ||
                    EF.Functions.Like(m.Email, term))
                .OrderBy(m => m.NomeCompleto)
                .ToListAsync();
        }

        public async Task UpdateAsync(Medico medico)
        {
            _context.Medicos.Update(medico);
            await _context.SaveChangesAsync();
        }
    }
}