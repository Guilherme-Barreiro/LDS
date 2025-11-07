namespace ConsultaPlus.API.DTOs.Pacientes
{
    public class PacienteResponseDto
    {
        public int Id { get; set; }
        public string? NomeCompleto { get; set; }
        public string? Nif { get; set; }
        public string NUtente { get; set; } = default!;
        public string? Telemovel { get; set; }
        public string? Morada { get; set; }
        public string? Email { get; set; }
        public DateTime? DataNascimento { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
