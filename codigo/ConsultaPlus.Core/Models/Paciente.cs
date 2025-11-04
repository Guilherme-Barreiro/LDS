using System.Collections.Generic;
using System;

namespace ConsultaPlus.Core.Models;

public class Paciente
{
    public int Id { get; set; }
    public string? NomeCompleto { get; set; }
    public string? Nif { get; set; }
    public string NUtente { get; set; }
    public string PasswordHash { get; set; }
    public string? Telemovel { get; set; }
    public string? Morada { get; set; }
    public string? Email { get; set; }
    public DateTime? DataNascimento { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}