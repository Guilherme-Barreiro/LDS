namespace ConsultaPlus.API.DTOs.Medicos
{
    public class CreateMedicoDto
    {
        public string NomeCompleto { get; set; } = string.Empty;
        public string Telemovel { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NUtente { get; set; } = string.Empty;
        public string Password { get; set; } = "TEMP_ONLY";
        public DateTime DataNascimento { get; set; }
    }
}
