using System;

namespace ConsultaPlus.Core.Models;

public class HorarioExcecaoMedico
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }
    public bool IsReducao { get; set; }
    public string? Motivo { get; set; }

    public int MedicoId { get; set; }
    public Medico Medico { get; set; }
}