using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IHorarioExcecaoMedico
    {
        Task RegistarExcecaoAsync(
        int medicoId, DateOnly data, TimeSpan horaInicio, TimeSpan horaFim,
        bool isReducao, string? motivo, CancellationToken ct);
    }
}
