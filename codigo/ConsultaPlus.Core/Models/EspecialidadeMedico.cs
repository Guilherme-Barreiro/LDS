namespace ConsultaPlus.Core.Models;

public class EspecialidadeMedico
{
    // Chave Estrangeira 
    public int MedicoId { get; set; }
    public int EspecialidadeId { get; set; }

    // Propriedades de Navegação 
    public Medico? Medico { get; set; }
    public Especialidade? Especialidade { get; set; }
}