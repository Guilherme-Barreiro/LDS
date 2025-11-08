using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsultaPlus.Core.Models.Relatorios;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IRelatorioService
    {
        Task<List<ConsultasPorPeriodo>> GetConsultasPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? medicoId = null);
        Task<TaxaNaoComparecimento> GetTaxaNaoComparecimentoAsync(DateTime? dataInicio = null, DateTime? dataFim = null, int? medicoId = null, int? especialidadeId = null);
    }
}
