namespace ConsultaPlus.API.DTOs.Medicos
{
    public class MedicoResponseDto
    {
        public int Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Telemovel { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NUtente { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
