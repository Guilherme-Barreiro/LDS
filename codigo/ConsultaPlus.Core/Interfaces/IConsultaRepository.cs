using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IConsultaRepository : IGenericRepository<Consulta>
    {
        Task<PagedResult<Consulta>> GetByPacienteAsync(
            int pacienteId,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<List<Consulta>> GetByMedicoRangeAsync(
            int medicoId,
            DateTime from,
            DateTime to,
            bool onlyConfirmed,
            CancellationToken ct = default);

        Task<IEnumerable<Consulta>> GetByMedicoIdAsync(int medicoId, CancellationToken ct = default);
        Task<IEnumerable<Consulta>> GetByPacienteIdAsync(int pacienteId, CancellationToken ct = default);
    }

    public record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);

}
