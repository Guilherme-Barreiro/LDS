using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Services
{
    public class DisponibilidadeService : IDisponibilidadeService
    {
        private readonly ApplicationDbContext _db;
        private const int SLOT_MINUTES = 30; // duração fixa

        public DisponibilidadeService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<DateTime>> GetSlotsLivresAsync(
            int medicoId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            if (toUtc <= fromUtc) return Array.Empty<DateTime>();

            static string DiaSemanaPt(DateTime d) => d.DayOfWeek switch
            {
                DayOfWeek.Monday => "Seg",
                DayOfWeek.Tuesday => "Ter",
                DayOfWeek.Wednesday => "Qua",
                DayOfWeek.Thursday => "Qui",
                DayOfWeek.Friday => "Sex",
                DayOfWeek.Saturday => "Sab",
                DayOfWeek.Sunday => "Dom",
                _ => "Seg"
            };

            var horarios = await _db.HorariosTrabalhoMedicos
                .Where(h => h.MedicoId == medicoId)
                .ToListAsync(ct);

            var excecoes = await _db.HorariosExcecaoMedicos
                .Where(x => x.MedicoId == medicoId &&
                            x.Data >= fromUtc.Date && x.Data <= toUtc.Date)
                .ToListAsync(ct);

            var consultas = await _db.Consultas
                .Where(c => c.MedicoId == medicoId &&
                            c.DataConsulta < toUtc &&
                            c.DataConsulta.AddMinutes(c.Duracao) > fromUtc)
                .Select(c => new
                {
                    Start = c.DataConsulta,
                    End = c.DataConsulta.AddMinutes(c.Duracao)
                })
                .ToListAsync(ct);

            var livres = new List<DateTime>();
            var cursor = RoundUpToSlot(fromUtc);

            while (cursor < toUtc)
            {
                var slotEnd = cursor.AddMinutes(SLOT_MINUTES);

                if (slotEnd.Date != cursor.Date)
                {
                    cursor = new DateTime(cursor.Year, cursor.Month, cursor.Day, 0, 0, 0, DateTimeKind.Utc)
                                 .AddDays(1);
                    continue;
                }

                var dpt = DiaSemanaPt(cursor);
                var horariosDoDia = horarios.Where(h => h.DiaSemana == dpt).ToList();

                bool dentroDeAlgumHorario = horariosDoDia.Any(h =>
                    cursor.TimeOfDay >= h.HoraInicio &&
                    slotEnd.TimeOfDay <= h.HoraFim);

                if (dentroDeAlgumHorario)
                {
                    var excDoDia = excecoes.Where(x => x.Data.Date == cursor.Date).ToList();

                    bool bloqueado = excDoDia.Any(x =>
                        !(slotEnd.TimeOfDay <= x.HoraInicio || cursor.TimeOfDay >= x.HoraFim));

                    if (!bloqueado)
                    {
                        bool ocupado = consultas.Any(c => c.Start < slotEnd && c.End > cursor);
                        if (!ocupado)
                            livres.Add(cursor);
                    }
                }

                cursor = cursor.AddMinutes(SLOT_MINUTES);
            }

            return livres;
        }

        public async Task<IReadOnlyList<DateTime>> GetProximosSlotsAsync(
            int medicoId, int count, CancellationToken ct = default)
        {
            if (count <= 0) count = 10;

            var start = DateTime.UtcNow;
            var end = start.AddDays(30);

            var slots = await GetSlotsLivresAsync(medicoId, start, end, ct);
            return slots.Take(count).ToList();
        }

        private static DateTime RoundUpToSlot(DateTime dt)
        {
            var minutesBucket = (dt.Minute / SLOT_MINUTES) * SLOT_MINUTES;
            var rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, minutesBucket, 0, DateTimeKind.Utc);
            if (rounded < dt) rounded = rounded.AddMinutes(SLOT_MINUTES);
            return rounded;
        }
    }
}
