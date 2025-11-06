using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Models.Relatorios
{
    public class ConsultasPorPeriodo
    {
        public string MedicoNome { get; set; }
        public string EspecialidadeNome { get; set; }
        public int TotalConsultas { get; set; }
        public int ConsultasRealizadas { get; set; }
        public int ConsultasNaoCompareceram { get; set; }
        public int ConsultasCanceladas { get; set; }
    }
}
