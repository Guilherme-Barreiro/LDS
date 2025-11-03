using System.Collections.Generic;
using System;

namespace ConsultaPlus.Core.Models;

public class Medico
{
	public int Id { get; set; }
	public string NomeCompleto { get; set; }
	public string Telemovel { get; set; }
	public string Email { get; set; }
	public string NUtente { get; set; }
	public string PasswordHash { get; set; }
	public DateTime DataNascimento { get; set; }
	public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

    // Propriedades de Navegação
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
	public ICollection<HorarioTrabalhoMedico> HorariosTrabalho { get; set; } = new List<HorarioTrabalhoMedico>();
	public ICollection<HorarioExcecaoMedico> HorariosExcecao { get; set; } = new List<HorarioExcecaoMedico>();
	public ICollection<EspecialidadeMedico> EspecialidadesMedico { get; set; } = new List<EspecialidadeMedico>();
}