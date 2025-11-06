using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Models.Relatorios
{
    public class TaxaNaoComparecimentoPorMedico
    {
        public string MedicoNome { get; set; }
        public string EspecialidadeNome { get; set; }
        public decimal Taxa { get; set; }
        public int TotalConsultas { get; set; }
        public int NaoCompareceram { get; set; }
    }
}
