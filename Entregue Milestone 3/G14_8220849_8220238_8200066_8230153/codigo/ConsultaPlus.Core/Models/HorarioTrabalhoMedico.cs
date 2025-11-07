using System;

namespace ConsultaPlus.Core.Models;

public class HorarioTrabalhoMedico
{
    public int Id { get; set; }
    public string DiaSemana { get; set; } 
    public TimeSpan HoraInicio { get; set; } 
    public TimeSpan HoraFim { get; set; }

    public int MedicoId { get; set; }
    public Medico Medico { get; set; }
}