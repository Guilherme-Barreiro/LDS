using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Services
{
    public class HorarioTrabalhoMedicoService : IHorarioTrabalhoMedico
    {
        private readonly ApplicationDbContext _db;
        private static readonly string[] DiasValidos = { "Seg", "Ter", "Qua", "Qui", "Sex", "Sab", "Dom" };

        public HorarioTrabalhoMedicoService(ApplicationDbContext db) => _db = db;

        public async Task<int> DefinirHorarioAsync(int medicoId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFim, CancellationToken ct)
        {
            Validar(diaSemana, horaInicio, horaFim);

            if (!await _db.Medicos.AnyAsync(m => m.Id == medicoId, ct))
                throw new KeyNotFoundException("Médico não encontrado.");

            var sobrepoe = await _db.HorariosTrabalhoMedicos
                .AnyAsync(h => h.MedicoId == medicoId && h.DiaSemana == diaSemana &&
                               h.HoraInicio < horaFim && h.HoraFim > horaInicio, ct);
            if (sobrepoe) throw new InvalidOperationException("Já existe um intervalo sobreposto para esse dia.");

            var novo = new HorarioTrabalhoMedico
            {
                MedicoId = medicoId,
                DiaSemana = diaSemana,
                HoraInicio = horaInicio,
                HoraFim = horaFim
            };

            _db.HorariosTrabalhoMedicos.Add(novo);
            await _db.SaveChangesAsync(ct);
            return novo.Id;
        }

        public async Task AtualizarHorarioAsync(int medicoId, int horarioId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFim, CancellationToken ct)
        {
            Validar(diaSemana, horaInicio, horaFim);

            var horario = await _db.HorariosTrabalhoMedicos.FirstOrDefaultAsync(h => h.Id == horarioId, ct)
                          ?? throw new KeyNotFoundException("Horário não encontrado.");
            if (horario.MedicoId != medicoId)
                throw new UnauthorizedAccessException("Horário não pertence a este médico.");

            var sobrepoe = await _db.HorariosTrabalhoMedicos
                .AnyAsync(h => h.MedicoId == medicoId && h.Id != horarioId && h.DiaSemana == diaSemana &&
                               h.HoraInicio < horaFim && h.HoraFim > horaInicio, ct);
            if (sobrepoe) throw new InvalidOperationException("Atualização causaria sobreposição.");

            horario.DiaSemana = diaSemana;
            horario.HoraInicio = horaInicio;
            horario.HoraFim = horaFim;

            await _db.SaveChangesAsync(ct);
        }

        private static void Validar(string diaSemana, TimeSpan inicio, TimeSpan fim)
        {
            if (!DiasValidos.Contains(diaSemana)) throw new ArgumentException("DiaSemana inválido.");
            if (fim <= inicio) throw new ArgumentException("HoraFim deve ser maior que HoraInicio.");
        }
    }
}
