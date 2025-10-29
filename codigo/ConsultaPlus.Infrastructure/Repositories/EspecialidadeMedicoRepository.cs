using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Core.Interfaces;

namespace ConsultaPlus.Infrastructure.Repositories
{
	public class EspecialidadeMedicoRepository : IEspecialidadeMedico
	{
		private readonly ConsultaPlusDbContext _context;

		public EspecialidadeMedicoRepository(ConsultaPlusDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId)
		{
			return await _context.EspecialidadesMedico
				.Where(em => em.EspecialidadeId == especialidadeId)
				.Select(em => em.Medico)
				.ToListAsync();
		}

		public async Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId)
		{
			return await _context.EspecialidadesMedico
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

		public async Task AddAsync(EspecialidadeMedico especialidadeMedico)
		{
			_context.EspecialidadesMedico.Add(especialidadeMedico);
			await _context.SaveChangesAsync();
		}

		public async Task RemoveAsync(int medicoId, int especialidadeId)
		{
			var entity = await _context.EspecialidadesMedico
				.FirstOrDefaultAsync(em => em.MedicoId == medicoId && em.EspecialidadeId == especialidadeId);

			if (entity != null)
			{
				_context.EspecialidadesMedico.Remove(entity);
				await _context.SaveChangesAsync();
			}
		}
	}
}
