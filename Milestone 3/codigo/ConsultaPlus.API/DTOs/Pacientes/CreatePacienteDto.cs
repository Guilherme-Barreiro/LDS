namespace ConsultaPlus.API.DTOs.Pacientes
{
    public class CreatePacienteDto
    {
        public string NUtente { get; set; } = default!;
        public string Password { get; set; } = default!;

        public string? NomeCompleto { get; set; }
        public string? Nif { get; set; }
        public string? Telemovel { get; set; }
        public string? Morada { get; set; }
        public string? Email { get; set; }
        public DateTime? DataNascimento { get; set; }
    }
}
