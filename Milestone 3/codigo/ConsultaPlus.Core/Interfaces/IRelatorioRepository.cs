using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsultaPlus.Core.Models.Relatorios;

namespace ConsultaPlus.Core.Interfaces
{
    public interface IRelatorioRepository
    {
        Task<List<ConsultasPorPeriodo>> GetConsultasPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? medicoId);
        Task<TaxaNaoComparecimento> GetTaxaNaoComparecimentoAsync(DateTime? dataInicio, DateTime? dataFim, int? medicoId, int? especialidadeId);
    }
}
