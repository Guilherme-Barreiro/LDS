using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class PacienteRepository : IPacienteRepository
    {
        private readonly ApplicationDbContext _context;

        public PacienteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Paciente paciente)
        {
            await _context.Pacientes.AddAsync(paciente);
            await _context.SaveChangesAsync();
        }

        public async Task<Paciente?> GetByNUtenteAsync(string nUtente)
        {
            return await _context.Pacientes.FirstOrDefaultAsync(p => p.NUtente == nUtente);
        }

        

        public async Task<Paciente?> GetByEmailAsync(string email)
        {
            return await _context.Pacientes.FirstOrDefaultAsync(p => p.Email == email);
        }

        public async Task UpdateAsync(Paciente paciente)
        {
            
            _context.Pacientes.Update(paciente);
            await _context.SaveChangesAsync();
        }

      
    }
}