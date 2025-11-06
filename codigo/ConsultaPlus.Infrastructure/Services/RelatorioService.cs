using ConsultaPlus.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsultaPlus.Core.Models.Relatorios;

namespace ConsultaPlus.Infrastructure.Services
{
    public class RelatorioService : IRelatorioService
    {
        private readonly IRelatorioRepository _repo;

        public RelatorioService(IRelatorioRepository repo)
        {
            _repo = repo;
        }
        public async Task<List<ConsultasPorPeriodo>> GetConsultasPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? medicoId = null)
        {
            if (dataInicio > dataFim)
                throw new ArgumentException("A data de início não pode ser posterior à data de fim.");

            return await _repo.GetConsultasPorPeriodoAsync(dataInicio, dataFim, medicoId);
        }

        public async Task<TaxaNaoComparecimento> GetTaxaNaoComparecimentoAsync(DateTime? dataInicio = null, DateTime? dataFim = null, int? medicoId = null, int? especialidadeId = null)
        {
            if (dataInicio.HasValue && dataFim.HasValue)
            {
                if ((dataFim.Value - dataInicio.Value).TotalDays > 365)
                    throw new ArgumentException("O período não pode exceder 1 ano.");
            }

            return await _repo.GetTaxaNaoComparecimentoAsync(dataInicio, dataFim, medicoId, especialidadeId);
        }
    }
}