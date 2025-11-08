using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models.Relatorios;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class RelatorioRepository : IRelatorioRepository
    {
        private readonly ApplicationDbContext _context;

        public RelatorioRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<ConsultasPorPeriodo>> GetConsultasPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? medicoId)
        {
            if ((dataFim - dataInicio).TotalDays > 365)
                throw new ArgumentException("O periodo nao pode exceder 1 ano.");

            var query = _context.Consultas
                .Include(c => c.Medico)
                .Include(c => c.Especialidade)
                .Where(c => c.DataConsulta >= dataInicio && c.DataConsulta <= dataFim);

            if (medicoId.HasValue)
                query = query.Where(c => c.MedicoId == medicoId.Value);

            var result = await query
                .GroupBy(c => new { c.MedicoId, c.Medico.NomeCompleto, c.Especialidade.Nome })
                .Select(g => new ConsultasPorPeriodo
                {
                    MedicoNome = g.Key.NomeCompleto,
                    EspecialidadeNome = g.Key.Nome,
                    TotalConsultas = g.Count(),
                    ConsultasRealizadas = g.Count(c => c.Estado == "Realizada"),
                    ConsultasNaoCompareceram = g.Count(c => c.Estado == "Nao Compareceu"),
                    ConsultasCanceladas = g.Count(c => c.Estado == "Cancelada")
                })
                .OrderByDescending(x => x.TotalConsultas)
                .ToListAsync();

            return result;
        }

        public async Task<TaxaNaoComparecimento> GetTaxaNaoComparecimentoAsync(DateTime? dataInicio, DateTime? dataFim, int? medicoId, int? especialidadeId)
        {
            var query = _context.Consultas
                .Include(c => c.Medico)
                .Include(c => c.Especialidade)
                .AsQueryable();

            if (dataInicio.HasValue)
                query = query.Where(c => c.DataConsulta >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(c => c.DataConsulta <= dataFim.Value);

            if (medicoId.HasValue)
                query = query.Where(c => c.MedicoId == medicoId.Value);

            if (especialidadeId.HasValue)
                query = query.Where(c => c.EspecialidadeId == especialidadeId.Value);

            var totalConsultas = await query.CountAsync();
            var totalNaoCompareceram = await query.CountAsync(c => c.Estado == "Nao Compareceu");
            var taxaGlobal = totalConsultas > 0 ? Math.Round((decimal)totalNaoCompareceram / totalConsultas * 100, 2) : 0;

            var porMedico = await query
                .GroupBy(c => new { c.MedicoId, c.Medico.NomeCompleto, c.Especialidade.Nome })
                .Select(g => new TaxaNaoComparecimentoPorMedico
                {
                    MedicoNome = g.Key.NomeCompleto,
                    EspecialidadeNome = g.Key.Nome,
                    TotalConsultas = g.Count(),
                    NaoCompareceram = g.Count(c => c.Estado == "Nao Compareceu"),
                    Taxa = g.Count() > 0 ? Math.Round((decimal)g.Count(c => c.Estado == "Nao Compareceu") / g.Count() * 100, 2) : 0
                })
                .OrderByDescending(x => x.Taxa)
                .ToListAsync();

            return new TaxaNaoComparecimento
            {
                TaxaGlobal = taxaGlobal,
                TotalConsultas = totalConsultas,
                TotalNaoCompareceram = totalNaoCompareceram,
                PorMedico = porMedico
            };
        }
    }
}
