using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class ConsultaRepository : GenericRepository<Consulta>, IConsultaRepository
    {
        public ConsultaRepository(ApplicationDbContext context) : base(context) { }

        public async Task<PagedResult<Consulta>> GetByPacienteAsync(
    int pacienteId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        {
            var q = _context.Set<Consulta>()
                .Include(c => c.Medico)
                .Include(c => c.Especialidade)
                .Include(c => c.Sala)
                .AsNoTracking()
                .Where(c => c.PacienteId == pacienteId);

            if (from.HasValue) q = q.Where(c => c.DataConsulta >= from.Value);
            if (to.HasValue) q = q.Where(c => c.DataConsulta <= to.Value);

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(c => c.DataConsulta)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Consulta>(total, page, pageSize, items);
        }

        public async Task<List<Consulta>> GetByMedicoRangeAsync(
            int medicoId, DateTime from, DateTime toExclusive, bool onlyConfirmed, CancellationToken ct = default)
        {
            var q = _context.Set<Consulta>()
                .Include(c => c.Paciente)
                .Include(c => c.Sala)
                .AsNoTracking()
                .Where(c => c.MedicoId == medicoId &&
                            c.DataConsulta >= from && c.DataConsulta <= toExclusive);

            if (onlyConfirmed)
                q = q.Where(c => c.Estado == "Confirmada"); 

            return await q.OrderBy(c => c.DataConsulta).ToListAsync(ct);
        }

        public async Task<IEnumerable<Consulta>> GetByMedicoIdAsync(int medicoId, CancellationToken ct = default)
        {
            return await _context.Consultas
                .Include(c => c.Paciente)
                .Include(c => c.Especialidade)
                .Include(c => c.Sala)
                .Where(c => c.MedicoId == medicoId)
                .OrderByDescending(c => c.DataConsulta)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Consulta>> GetByPacienteIdAsync(int pacienteId, CancellationToken ct = default)
        {
            return await _context.Consultas
                .Include(c => c.Medico)
                .Include(c => c.Especialidade)
                .Include(c => c.Sala)
                .Where(c => c.PacienteId == pacienteId)
                .OrderByDescending(c => c.DataConsulta)
                .ToListAsync(ct);
        }
    }
}
