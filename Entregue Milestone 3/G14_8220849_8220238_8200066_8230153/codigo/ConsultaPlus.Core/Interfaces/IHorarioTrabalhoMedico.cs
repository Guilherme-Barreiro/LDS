using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IHorarioTrabalhoMedico
    {
        Task<int> DefinirHorarioAsync(int medicoId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFim, CancellationToken ct);
        Task AtualizarHorarioAsync(int medicoId, int horarioId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFim, CancellationToken ct);
    }
}
