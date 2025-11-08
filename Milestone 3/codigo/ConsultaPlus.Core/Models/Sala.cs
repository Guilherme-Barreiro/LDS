using System.Collections.Generic;

namespace ConsultaPlus.Core.Models;

public class Sala
{
    public int Id { get; set; }
    public string Nome { get; set; }

    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}