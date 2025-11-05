namespace ConsultaPlus.API.DTOs.Consultas
{
    public class CreateConsultaDto
    {
        public int PacienteId { get; set; }
        public int MedicoId { get; set; }
        public int SalaId { get; set; }
        public int EspecialidadeId { get; set; }
        public DateTime DataConsulta { get; set; }
        public int Duracao { get; set; }
    }
}
