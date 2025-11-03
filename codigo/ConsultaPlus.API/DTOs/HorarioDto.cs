namespace ConsultaPlus.API.DTOs
{
    public class HorarioDto
    {
        public int Id { get; set; }
        public int MedicoId { get; set; }
        public string DiaSemana { get; set; } = default!;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
    }
}
