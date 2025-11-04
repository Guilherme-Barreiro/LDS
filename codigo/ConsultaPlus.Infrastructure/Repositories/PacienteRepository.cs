using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class PacienteRepository : IPacienteRepository
    {
        private readonly ApplicationDbContext _context;
        public PacienteRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Paciente>> GetAllAsync() =>
            await _context.Pacientes.AsNoTracking().OrderBy(p => p.Id).ToListAsync();

        public async Task<Paciente?> GetByIdAsync(int id) =>
            await _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Paciente?> GetByNUtenteAsync(string nUtente) =>
            await _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.NUtente == nUtente);

        public async Task AddAsync(Paciente paciente)
        {
            await _context.Pacientes.AddAsync(paciente);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Paciente paciente)
        {
            _context.Pacientes.Update(paciente);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null) return;
            _context.Pacientes.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
