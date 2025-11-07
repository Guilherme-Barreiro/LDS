using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class NotificacaoRepository : GenericRepository<Notificacao>, INotificacaoRepository
    {
        public NotificacaoRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Notificacao>> GetByMedicoAsync(int medicoId, bool unreadOnly = false)
        {
            var q = _context.Notificacoes.AsNoTracking().Where(n => n.MedicoId == medicoId);
            if (unreadOnly) q = q.Where(n => !n.Lida);
            return await q.OrderByDescending(n => n.DataCriacao).ToListAsync();
        }

        public async Task<IEnumerable<Notificacao>> GetByPacienteAsync(int pacienteId, bool unreadOnly = false)
        {
            var q = _context.Notificacoes.AsNoTracking().Where(n => n.PacienteId == pacienteId);
            if (unreadOnly) q = q.Where(n => !n.Lida);
            return await q.OrderByDescending(n => n.DataCriacao).ToListAsync();
        }

        public async Task<bool> MarcarComoLidaAsync(int id, bool lida = true)
        {
            var n = await _context.Notificacoes.FirstOrDefaultAsync(x => x.Id == id);
            if (n is null) return false;
            n.Lida = lida;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string categoria, string descricao, int? medicoId, int? pacienteId)
        {
            return await _context.Notificacoes.AsNoTracking().AnyAsync(n =>
                n.Categoria == categoria &&
                n.Descricao == descricao &&
                n.MedicoId == medicoId &&
                n.PacienteId == pacienteId);
        }
    }
}
