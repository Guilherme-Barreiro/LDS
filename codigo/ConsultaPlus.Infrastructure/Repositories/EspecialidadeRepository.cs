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
    public class EspecialidadeRepository : IEspecialidadeCRUD
    {
        private readonly ApplicationDbContext _context;

        public EspecialidadeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Especialidade>> GetAllAsync()
        {
            return await _context.Especialidades
                                 .AsNoTracking()
                                 .OrderBy(e => e.Nome)
                                 .ToListAsync();
        }

        public async Task<Especialidade?> GetByIdAsync(int id)
        {
            return await _context.Especialidades
                                 .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AddAsync(Especialidade especialidade)
        {
            var existe = await _context.Especialidades
                                      .AsNoTracking()
                                      .AnyAsync(e => e.Nome.ToLower() == especialidade.Nome.ToLower());

            if (existe)
                throw new InvalidOperationException("Já existe uma especialidade com esse nome.");

            _context.Especialidades.Add(especialidade);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Especialidade especialidade, string newNome)
        {
            if (especialidade == null)
                throw new ArgumentNullException(nameof(especialidade));

            var nome = newNome?.Trim();
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome inválido.", nameof(newNome));

            var existente = await _context.Especialidades.FindAsync(especialidade.Id);
            if (existente == null)
                throw new KeyNotFoundException("Especialidade não encontrada.");

            var nomeEmUso = await _context.Especialidades
                                         .AsNoTracking()
                                         .AnyAsync(e => e.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)
                                                        && e.Id != especialidade.Id);

            if (nomeEmUso)
                throw new InvalidOperationException("Já existe outra especialidade com esse nome.");

            existente.Nome = nome;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Especialidades.FindAsync(id);
            if (entity == null)
                return;

            _context.Especialidades.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
