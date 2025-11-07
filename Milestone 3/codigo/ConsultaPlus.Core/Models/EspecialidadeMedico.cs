namespace ConsultaPlus.Core.Models;

public class EspecialidadeMedico
{
    public int MedicoId { get; set; }
    public int EspecialidadeId { get; set; }

    public Medico? Medico { get; set; }
    public Especialidade? Especialidade { get; set; }
}