using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IHorarioTrabalhoMedico
    {
        Task DefinirHorarioAsync(
       int medicoId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFim, CancellationToken ct);
    }
}
