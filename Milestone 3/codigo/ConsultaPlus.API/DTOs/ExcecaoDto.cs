namespace ConsultaPlus.API.DTOs
{
    public class ExcecaoDto
    {
        public int Id { get; set; }
        public int MedicoId { get; set; }
        public DateOnly Data { get; set; }         
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public bool IsReducao { get; set; }
        public string? Motivo { get; set; }
    }
}
