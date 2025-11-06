using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Models.Relatorios
{
    public class TaxaNaoComparecimento
    {
        public decimal TaxaGlobal { get; set; }
        public int TotalConsultas { get; set; }
        public int TotalNaoCompareceram { get; set; }
        public List<TaxaNaoComparecimentoPorMedico> PorMedico { get; set; } = new();
    }
}
