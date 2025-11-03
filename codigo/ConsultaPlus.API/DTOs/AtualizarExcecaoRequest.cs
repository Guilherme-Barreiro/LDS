using System.ComponentModel.DataAnnotations;

namespace ConsultaPlus.API.DTOs
{
    public class AtualizarExcecaoRequest
    {
        [Required] public DateOnly Data { get; set; }
        [Required] public TimeSpan HoraInicio { get; set; }
        [Required] public TimeSpan HoraFim { get; set; }
        public bool IsReducao { get; set; }
        public string? Motivo { get; set; }
    }
}
