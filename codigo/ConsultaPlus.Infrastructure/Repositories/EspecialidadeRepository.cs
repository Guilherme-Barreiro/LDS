using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class EspecialidadeRepository : GenericRepository<Especialidade>, IEspecialidadeRepository
    {
        private readonly ApplicationDbContext _context;

        public EspecialidadeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Especialidade>> SearchByNameAsync(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return await GetAllAsync();

            return await _context.Especialidades
                .AsNoTracking()
                .Where(e => e.Nome.ToLower().Contains(nome.ToLower()))
                .OrderBy(e => e.Nome)
                .ToListAsync();
        }

        public async Task<bool> IsLinkedToMedic(int especialidadeId)
        {

            return await _context.EspecialidadesMedico
                .AsNoTracking()
                .AnyAsync(em => em.EspecialidadeId == especialidadeId);
        }

        public async Task<bool> ExistsByNameAsync(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return false;

            var nomeTrim = nome.Trim();
            return await _context.Especialidades
                .AsNoTracking()
                .AnyAsync(e => e.Nome.Equals(nomeTrim, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> ExistsByNameAndNotIdAsync(string nome, int id)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return false;

            var nomeTrim = nome.Trim();
            return await _context.Especialidades
                .AsNoTracking()
                .AnyAsync(e => e.Nome.Equals(nomeTrim, StringComparison.OrdinalIgnoreCase)
                            && e.Id != id);
        }
    }
}
