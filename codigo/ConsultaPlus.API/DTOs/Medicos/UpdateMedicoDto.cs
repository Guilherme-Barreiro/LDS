namespace ConsultaPlus.API.DTOs.Medicos
{
    public class UpdateMedicoDto
    {
        public string NomeCompleto { get; set; } = string.Empty;
        public string Telemovel { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
    }
}
