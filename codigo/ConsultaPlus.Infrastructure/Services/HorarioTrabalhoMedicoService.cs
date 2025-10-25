using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Threading;               


namespace ConsultaPlus.Infrastructure.Services
{
    public class HorarioTrabalhoMedicoService : IHorarioTrabalhoMedico
    {
        private readonly ApplicationDbContext _db;
        private static readonly string[] DiasValidos = { "Seg", "Ter", "Qua", "Qui", "Sex", "Sab", "Dom" };

        public HorarioTrabalhoMedicoService(ApplicationDbContext db) => _db = db;

        public async Task DefinirHorarioAsync(int medicoId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFim, CancellationToken ct)
        {
            if (!DiasValidos.Contains(diaSemana))
                throw new ArgumentException("DiaSemana inválido. Use: Seg, Ter, Qua, Qui, Sex, Sab, Dom.");
            if (horaFim <= horaInicio)
                throw new ArgumentException("HoraFim deve ser maior que HoraInicio.");

            var existeMedico = await _db.Medicos.AnyAsync(m => m.Id == medicoId, ct);
            if (!existeMedico) throw new KeyNotFoundException("Médico não encontrado.");

            // impedir sobreposição no mesmo dia para este médico
            var sobrepoe = await _db.HorariosTrabalhoMedicos
                .AnyAsync(h => h.MedicoId == medicoId && h.DiaSemana == diaSemana &&
                               h.HoraInicio < horaFim && h.HoraFim > horaInicio, ct);
            if (sobrepoe)
                throw new InvalidOperationException("Já existe um intervalo sobreposto para esse dia.");

            _db.HorariosTrabalhoMedicos.Add(new HorarioTrabalhoMedico
            {
                MedicoId = medicoId,
                DiaSemana = diaSemana,
                HoraInicio = horaInicio,
                HoraFim = horaFim
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
