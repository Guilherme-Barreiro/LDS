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

        public Task<Medico?> GetByEmailAsync(string email) =>
            _context.Medicos.AsNoTracking().FirstOrDefaultAsync(m => m.Email == email);
    }
}
