using ConsultaPlus.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface INotificacaoRepository : IGenericRepository<Notificacao>
    {
        Task<IEnumerable<Notificacao>> GetByMedicoAsync(int medicoId, bool unreadOnly = false);
        Task<IEnumerable<Notificacao>> GetByPacienteAsync(int pacienteId, bool unreadOnly = false);
        Task<bool> MarcarComoLidaAsync(int id, bool lida = true);
    }
}
