using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class SalaRepository : GenericRepository<Sala>, ISalaRepository
    {
        public SalaRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Sala>> SearchByNameAsync(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return Enumerable.Empty<Sala>();

            var term = $"%{nome.Trim()}%";
            return await _context.Salas
                .AsNoTracking()
                .Where(s => EF.Functions.Like(s.Nome, term))
                .OrderBy(s => s.Nome)
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string nome)
        {
            var n = (nome ?? string.Empty).Trim();
            return await _context.Salas.AsNoTracking()
                .AnyAsync(s => s.Nome.ToLower() == n.ToLower());
        }

        public async Task<bool> HasFutureConsultasAsync(int salaId, DateTime now)
        {
            return await _context.Consultas.AsNoTracking()
                .AnyAsync(c => c.SalaId == salaId && c.DataConsulta > now);                                                                       
        }
    }
}
