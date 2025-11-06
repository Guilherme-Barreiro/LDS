using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IDisponibilidadeService
    {
        Task<IReadOnlyList<DateTime>> GetSlotsLivresAsync(
            int medicoId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default);

        Task<IReadOnlyList<DateTime>> GetProximosSlotsAsync(
            int medicoId,
            int count,
            CancellationToken ct = default);
    }
}
