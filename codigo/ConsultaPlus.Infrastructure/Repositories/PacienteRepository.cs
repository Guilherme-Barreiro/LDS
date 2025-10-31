using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class PacienteRepository : GenericRepository<Paciente>, IPacienteRepository
    {
        public PacienteRepository(ApplicationDbContext context) : base(context) { }

        public Task<Paciente?> GetByNUtenteAsync(string nUtente) =>
            _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.NUtente == nUtente);
    }
}
