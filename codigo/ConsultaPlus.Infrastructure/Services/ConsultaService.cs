using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Services
{
    public class ConsultaService : IConsultaService
    {
        private readonly IConsultaRepository _consultas;
        private readonly IMedicoRepository _medicos;
        private readonly IPacienteRepository _pacientes;
        private readonly ISalaRepository _salas;
        private readonly IEspecialidadeService _especialidades;
        private readonly ApplicationDbContext _db;

        public ConsultaService(
            IConsultaRepository consultas,
            IMedicoRepository medicos,
            IPacienteRepository pacientes,
            ISalaRepository salas,
            IEspecialidadeService especialidades,
            ApplicationDbContext db)
        {
            _consultas = consultas;
            _medicos = medicos;
            _pacientes = pacientes;
            _salas = salas;
            _especialidades = especialidades;
            _db = db;
        }

        public Task<Consulta?> GetByIdAsync(int id, CancellationToken ct = default)
            => _consultas.GetByIdAsync(id);

        public Task<IEnumerable<Consulta>> GetAllAsync(CancellationToken ct = default)
            => _consultas.GetAllAsync();

        public async Task<IEnumerable<Consulta>> GetByMedicoAsync(int medicoId, CancellationToken ct = default)
        {
            var all = await _consultas.GetByMedicoIdAsync(medicoId);
            return all.Where(c => c.MedicoId == medicoId);
        }

        public async Task<IEnumerable<Consulta>> GetByPacienteAsync(int pacienteId, CancellationToken ct = default)
        {
            var all = await _consultas.GetByPacienteIdAsync(pacienteId);
            return all.Where(c => c.PacienteId == pacienteId);
        }

        public async Task<Consulta> CreateAsync(Consulta nova, CancellationToken ct = default)
        {
            // --- Normalizar data para UTC e bloquear passado ---
            var dataUtc = nova.DataConsulta.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(nova.DataConsulta, DateTimeKind.Utc)
                : nova.DataConsulta.ToUniversalTime();

            if (dataUtc < DateTime.UtcNow)
                throw new ArgumentException("Nao e possivel marcar consultas no passado.");

            // --- Regra 30 minutos: só hh:00 e hh:30; segundos têm de ser 0 (ms podem existir) ---
            if (dataUtc.Second != 0 || (dataUtc.Minute % 30) != 0)
                throw new ArgumentException("A consulta deve começar em intervalos de 30 minutos (hh:00 ou hh:30).");

            var start = dataUtc;
            var end = start.AddMinutes(30); // duração fixa

            // Guardar a data normalizada
            nova.DataConsulta = start;

            // --- Entidades existem ---
            if (await _pacientes.GetByIdAsync(nova.PacienteId) is null)
                throw new ArgumentException($"PacienteId {nova.PacienteId} nao existe.");

            if (await _medicos.GetByIdAsync(nova.MedicoId) is null)
                throw new ArgumentException($"MedicoId {nova.MedicoId} nao existe.");

            if (await _especialidades.GetByIdAsync(nova.EspecialidadeId) is null)
                throw new ArgumentException($"EspecialidadeId {nova.EspecialidadeId} nao existe.");

            if (nova.SalaId != 0 && await _salas.GetByIdAsync(nova.SalaId) is null)
                throw new ArgumentException($"SalaId {nova.SalaId} nao existe.");

            // --- Médico tem a especialidade pedida ---
            var medicoTemEspecialidade = await _db.EspecialidadesMedico
                .AsNoTracking()
                .AnyAsync(em => em.MedicoId == nova.MedicoId &&
                                em.EspecialidadeId == nova.EspecialidadeId, ct);

            if (!medicoTemEspecialidade)
                throw new ArgumentException("O medico nao possui a especialidade selecionada.");

            // --- Dentro do horário de trabalho do médico (respeitando exceções) ---
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

            var dia = DiaSemanaPt(start);

            // horários do dia
            var horariosDia = await _db.HorariosTrabalhoMedicos
                .AsNoTracking()
                .Where(h => h.MedicoId == nova.MedicoId && h.DiaSemana == dia)
                .ToListAsync(ct);

            var dentroHorario = horariosDia.Any(h =>
                start.TimeOfDay >= h.HoraInicio &&
                end.TimeOfDay <= h.HoraFim);

            if (!dentroHorario)
                throw new ArgumentException("Fora do horario de trabalho do medico.");

            // exceções no dia (tratamos qualquer exceção como bloqueio se houver sobreposição)
            var excecoesDia = await _db.HorariosExcecaoMedicos
                .AsNoTracking()
                .Where(x => x.MedicoId == nova.MedicoId && x.Data.Date == start.Date)
                .ToListAsync(ct);

            var bloqueadoPorExcecao = excecoesDia.Any(x =>
                !(end.TimeOfDay <= x.HoraInicio ||
                  start.TimeOfDay >= x.HoraFim));

            if (bloqueadoPorExcecao)
                throw new ArgumentException("Indisponivel devido a excecao na agenda do medico.");

            // --- Sem sobreposição com consultas do mesmo médico (qualquer especialidade) ---
            var overlap = await _db.Consultas
                .AsNoTracking()
                .AnyAsync(c => c.MedicoId == nova.MedicoId &&
                               c.DataConsulta < end &&
                               c.DataConsulta.AddMinutes(c.Duracao) > start, ct);

            if (overlap)
                throw new ArgumentException("O medico já tem uma consulta nesse horario.");

            // --- Duração fixa + estado ---
            nova.Duracao = 30;
            nova.Estado = "Confirmada";

            await _consultas.AddAsync(nova);
            return nova;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _consultas.GetByIdAsync(id);
            if (existing is null) throw new KeyNotFoundException($"Consulta {id} nao existe.");
            await _consultas.DeleteAsync(id);
        }
    }
}
