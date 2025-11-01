using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



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
    }
}
