namespace ConsultaPlus.API.DTOs.Sns
{
    public class SnsPacienteResponseDto
    {
        public int Id { get; set; }
        public string NUtente { get; set; } = default!;
        public string NomeCompleto { get; set; } = default!;
        public string Nif { get; set; } = default!;
        public string Telemovel { get; set; } = default!;
        public string Morada { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime DataNascimento { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
