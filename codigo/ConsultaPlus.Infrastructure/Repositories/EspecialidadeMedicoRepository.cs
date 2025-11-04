using ConsultaPlus.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Core.Interfaces;

namespace ConsultaPlus.Infrastructure.Repositories
{
	public class EspecialidadeMedicoRepository : GenericRepository<EspecialidadeMedico>, IEspecialidadeMedicoRepository
	{
		private readonly ApplicationDbContext _context;

		public EspecialidadeMedicoRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId)
		{
            return await _context.EspecialidadesMedico
                .AsNoTracking()
                .Where(em => em.EspecialidadeId == especialidadeId)
                .Select(em => em.Medico)
                .ToListAsync();
		}

		public async Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId)
		{
            return await _context.EspecialidadesMedico
                .AsNoTracking()
                .Where(em => em.MedicoId == medicoId)
                .Select(em => em.Especialidade)
                .ToListAsync();
        }

		public async Task<bool> MedicoExistsAsync(int medicoId)
		{
			return await _context.Medicos
				.AsNoTracking()
				.AnyAsync(m => m.Id == medicoId);
		}

		public async Task<bool> EspecialidadeExistsAsync(int especialidadeId)
		{
			return await _context.Especialidades
				.AsNoTracking()
				.AnyAsync(e => e.Id == especialidadeId);
		}

		public async Task<bool> ExistsAsync(int medicoId, int especialidadeId)
		{
			return await _context.EspecialidadesMedico
				.AsNoTracking()
				.AnyAsync(em => em.MedicoId == medicoId && em.EspecialidadeId == especialidadeId);
		}

		public async Task DeleteAsync(int medicoId, int especialidadeId)
		{
			var assoc = await _context.EspecialidadesMedico
				.FirstOrDefaultAsync(em => em.MedicoId == medicoId && em.EspecialidadeId == especialidadeId);
			if (assoc != null)
                throw new KeyNotFoundException("Associação não encontrada.");
           
			_context.EspecialidadesMedico.Remove(assoc);
			
        }
    }
}
