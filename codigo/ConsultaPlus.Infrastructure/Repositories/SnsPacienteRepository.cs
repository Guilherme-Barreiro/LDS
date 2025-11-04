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
    public class SnsPacienteRepository : GenericRepository<SnsPaciente>, ISnsPacienteRepository
    {
        public SnsPacienteRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<SnsPaciente>> SearchByNomeAsync(string termo)
        {
            if (string.IsNullOrWhiteSpace(termo))
                return Enumerable.Empty<SnsPaciente>();

            var like = $"%{termo.Trim()}%";
            return await _context.SnsPacientes
                .AsNoTracking()
                .Where(p => EF.Functions.Like(p.NomeCompleto!, like))
                .OrderBy(p => p.NomeCompleto)
                .ToListAsync();
        }

        public Task<SnsPaciente?> GetByEmailAsync(string email) =>
            _context.SnsPacientes.AsNoTracking().FirstOrDefaultAsync(p => p.Email == email);
    }
}
