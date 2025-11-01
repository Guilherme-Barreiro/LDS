using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class EspecialidadeRepository : IEspecialidadeCRUD
    {
        private readonly ApplicationDbContext _ctx;
        public EspecialidadeRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<IEnumerable<Especialidade>> GetAllAsync() =>
            await _ctx.Especialidades.AsNoTracking().ToListAsync();

        public Task<Especialidade?> GetByIdAsync(int id) =>
            _ctx.Especialidades.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        public async Task AddAsync(Especialidade especialidade)
        {
            _ctx.Especialidades.Add(especialidade);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Especialidade especialidade)
        {
            _ctx.Especialidades.Update(especialidade);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var e = await _ctx.Especialidades.FindAsync(id);
            if (e is null) return;
            _ctx.Especialidades.Remove(e);
            await _ctx.SaveChangesAsync();
        }
    }
}
