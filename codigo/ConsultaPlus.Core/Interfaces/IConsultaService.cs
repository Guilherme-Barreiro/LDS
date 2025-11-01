using ConsultaPlus.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IConsultaService
    {
        Task<Consulta> CreateAsync(Consulta nova, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        Task<Consulta?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<Consulta>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<Consulta>> GetByMedicoAsync(int medicoId, CancellationToken ct = default);
        Task<IEnumerable<Consulta>> GetByPacienteAsync(int pacienteId, CancellationToken ct = default);
    }
}
