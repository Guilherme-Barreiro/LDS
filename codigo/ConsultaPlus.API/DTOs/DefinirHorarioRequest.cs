using System.ComponentModel.DataAnnotations;

namespace ConsultaPlus.API.DTOs
{
    public class DefinirHorarioRequest
    {
        // "Seg","Ter","Qua","Qui","Sex","Sab","Dom"
        [Required] public string DiaSemana { get; set; } = default!;
        [Required] public TimeSpan HoraInicio { get; set; }
        [Required] public TimeSpan HoraFim { get; set; }
    }
}
