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
    public class HorarioExcecaoMedicoService : IHorarioExcecaoMedico
    {
        private readonly ApplicationDbContext _db;
        public HorarioExcecaoMedicoService(ApplicationDbContext db) => _db = db;

        public async Task RegistarExcecaoAsync(int medicoId, DateOnly data, TimeSpan horaInicio, TimeSpan horaFim, bool isReducao, string? motivo, CancellationToken ct)
        {
            if (horaFim <= horaInicio)
                throw new ArgumentException("HoraFim deve ser maior que HoraInicio.");

            var existeMedico = await _db.Medicos.AnyAsync(m => m.Id == medicoId, ct);
            if (!existeMedico) throw new KeyNotFoundException("Médico não encontrado.");

            var dataDate = data.ToDateTime(TimeOnly.MinValue).Date;

            var dup = await _db.HorariosExcecaoMedicos
                .AnyAsync(e =>
                    e.MedicoId == medicoId &&
                    e.Data == dataDate &&
                    e.HoraInicio == horaInicio &&
                    e.HoraFim == horaFim &&
                    e.IsReducao == isReducao, ct);

            if (!dup)
            {
                dup = _db.HorariosExcecaoMedicos
                    .AsEnumerable() 
                    .Any(e =>
                        e.MedicoId == medicoId &&
                        e.Data == dataDate &&
                        e.HoraInicio == horaInicio &&
                        e.HoraFim == horaFim &&
                        e.IsReducao == isReducao);
            }

            if (dup) return;


            _db.HorariosExcecaoMedicos.Add(new HorarioExcecaoMedico
            {
                MedicoId = medicoId,
                Data = dataDate,
                HoraInicio = horaInicio,
                HoraFim = horaFim,
                IsReducao = isReducao,
                Motivo = motivo
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
