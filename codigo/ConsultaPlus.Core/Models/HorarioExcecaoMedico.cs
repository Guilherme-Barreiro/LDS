using System;

namespace ConsultaPlus.Core.Models;

public class HorarioExcecaoMedico
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }
    public bool IsReducao { get; set; } // true se for para bloquear, false se for para abrir horário
    public string? Motivo { get; set; }

    // Chave Estrangeira 
    public int MedicoId { get; set; }
    // Propriedade de Navegação
    public Medico Medico { get; set; }
}