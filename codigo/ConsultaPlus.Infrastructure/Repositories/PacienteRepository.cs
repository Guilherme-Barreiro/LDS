using System.Collections.Generic;
using System.Linq;
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

        public async Task AddAsync(Paciente paciente)
        {
            await _context.Pacientes.AddAsync(paciente);
            await _context.SaveChangesAsync();
        }

        public async Task<Paciente?> GetByIdAsync(int id) =>
            await _context.Pacientes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Paciente?> GetByNUtenteAsync(string nUtente) =>
            await _context.Pacientes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.NUtente == nUtente);

        public async Task<Paciente?> GetByEmailAsync(string email) =>
            await _context.Pacientes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email);

        public async Task UpdateAsync(Paciente paciente)
        {
            _context.Pacientes.Update(paciente);
            await _context.SaveChangesAsync();
        }
    }
}