using System;

namespace ConsultaPlus.Core.Models;

public class Notificacao
{
    public int Id { get; set; }
    public string Categoria { get; set; } 
    public string Descricao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Chaves Estrangeiras - Uma notificação pode ser para um médico OU para um paciente
    public int? MedicoId { get; set; }
    public int? PacienteId { get; set; }

    // Propriedades de Navegação
    public Medico? Medico { get; set; }
    public Paciente? Paciente { get; set; }
}