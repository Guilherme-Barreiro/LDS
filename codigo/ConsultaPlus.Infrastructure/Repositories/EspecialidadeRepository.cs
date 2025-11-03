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

        public Task AddAsync(Especialidade especialidade)
        {
            _ctx.Especialidades.Add(especialidade);
            return Task.CompletedTask; 
        }

        public Task UpdateAsync(Especialidade especialidade)
        {
            _ctx.Especialidades.Update(especialidade);
            return Task.CompletedTask; 
        }

        public async Task DeleteAsync(int id)
        {
            var e = await _ctx.Especialidades.FindAsync(id);
            if (e is null) return;
            _ctx.Especialidades.Remove(e);
        }

        public async Task<bool> ExistsByNameAsync(string nome)
        {
            var n = (nome ?? "").Trim();
            return await _ctx.Especialidades.AsNoTracking()
                .AnyAsync(e => e.Nome.ToLower() == n.ToLower());
        }

        public Task<bool> HasMedicosAsync(int especialidadeId)
        {        
            return _ctx.EspecialidadesMedico.AsNoTracking()
                .AnyAsync(em => em.EspecialidadeId == especialidadeId);  
        }
    }
}
