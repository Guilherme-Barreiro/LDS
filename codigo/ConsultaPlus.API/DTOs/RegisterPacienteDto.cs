using System;

namespace ConsultaPlus.API.DTOs
{
    public class RegisterPacienteDto
    {
        public string NomeCompleto { get; set; }
        public string? Nif { get; set; }
        public string NUtente { get; set; }
        public string Password { get; set; }
        public string? Telemovel { get; set; }
        public string? Morada { get; set; }
        public string Email { get; set; }
        public DateTime DataNascimento { get; set; }
    }
}