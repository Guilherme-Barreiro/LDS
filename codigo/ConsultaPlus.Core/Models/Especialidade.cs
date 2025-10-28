using ConsultaPlus.Core.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ConsultaPlus.Core.Models;

public class Especialidade
{
    public int Id { get; set; }
    public required string Nome { get; set; }

    // Propriedades de Navegação
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
    public ICollection<EspecialidadeMedico> EspecialidadesMedico { get; set; } = new List<EspecialidadeMedico>();
}

