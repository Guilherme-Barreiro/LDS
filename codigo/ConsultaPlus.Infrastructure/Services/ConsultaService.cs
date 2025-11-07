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
        private readonly INotificacaoRepository _notificacoes;
        private readonly ApplicationDbContext _db;

        public ConsultaService(
            IConsultaRepository consultas,
            IMedicoRepository medicos,
            IPacienteRepository pacientes,
            ISalaRepository salas,
            IEspecialidadeService especialidades,
            ApplicationDbContext db,
            INotificacaoRepository notificacoes)
        {
            _consultas = consultas;
            _medicos = medicos;
            _pacientes = pacientes;
            _salas = salas;
            _especialidades = especialidades;
            _db = db;
            _notificacoes = notificacoes;
        }

        public Task<Consulta?> GetByIdAsync(int id, CancellationToken ct = default)
            => _consultas.GetByIdAsync(id);

        public Task<IEnumerable<Consulta>> GetAllAsync(CancellationToken ct = default)
            => _consultas.GetAllAsync();

        public async Task<IEnumerable<Consulta>> GetByMedicoAsync(int medicoId, CancellationToken ct = default)
        {
            var all = await _consultas.GetAllAsync();
            return all.Where(c => c.MedicoId == medicoId);
        }

        public async Task<IEnumerable<Consulta>> GetByPacienteAsync(int pacienteId, CancellationToken ct = default)
        {
            var all = await _consultas.GetAllAsync();
            return all.Where(c => c.PacienteId == pacienteId);
        }

        public async Task<Consulta> CreateAsync(Consulta nova, CancellationToken ct = default)
        {
            var dataUtc = nova.DataConsulta.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(nova.DataConsulta, DateTimeKind.Utc)
                : nova.DataConsulta.ToUniversalTime();

            if (dataUtc < DateTime.UtcNow)
                throw new ArgumentException("Nao e possivel marcar consultas no passado.");

            if (dataUtc.Second != 0 || (dataUtc.Minute % 30) != 0)
                throw new ArgumentException("A consulta deve comecar em intervalos de 30 minutos (hh:00 ou hh:30).");

            var start = dataUtc;
            var end = start.AddMinutes(30);

            nova.DataConsulta = start;

            if (await _pacientes.GetByIdAsync(nova.PacienteId) is null)
                throw new ArgumentException($"PacienteId {nova.PacienteId} nao existe.");

            if (await _medicos.GetByIdAsync(nova.MedicoId) is null)
                throw new ArgumentException($"MedicoId {nova.MedicoId} nao existe.");

            if (await _especialidades.GetByIdAsync(nova.EspecialidadeId) is null)
                throw new ArgumentException($"EspecialidadeId {nova.EspecialidadeId} nao existe.");

            var medicoTemEspecialidade = await _db.EspecialidadesMedico
                .AsNoTracking()
                .AnyAsync(em => em.MedicoId == nova.MedicoId &&
                                em.EspecialidadeId == nova.EspecialidadeId, ct);

            if (!medicoTemEspecialidade)
                throw new ArgumentException("O medico nao possui a especialidade selecionada.");

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

            var horariosDia = await _db.HorariosTrabalhoMedicos
                .AsNoTracking()
                .Where(h => h.MedicoId == nova.MedicoId && h.DiaSemana == dia)
                .ToListAsync(ct);

            var excecoesDia = await _db.HorariosExcecaoMedicos
                .AsNoTracking()
                .Where(x => x.MedicoId == nova.MedicoId && x.Data.Date == start.Date)
                .ToListAsync(ct);

            bool dentroHorarioBase = horariosDia.Any(h =>
                start.TimeOfDay >= h.HoraInicio &&
                end.TimeOfDay <= h.HoraFim);

            bool dentroExcecaoExtra = excecoesDia.Any(x =>
                !x.IsReducao &&
                !(end.TimeOfDay <= x.HoraInicio || start.TimeOfDay >= x.HoraFim));

            bool dentroHorarioFinal = dentroHorarioBase || dentroExcecaoExtra;

            if (!dentroHorarioFinal)
                throw new ArgumentException("Fora do horário de trabalho do médico.");

            var bloqueadoPorExcecao = excecoesDia.Any(x =>
                x.IsReducao &&
                !(end.TimeOfDay <= x.HoraInicio || start.TimeOfDay >= x.HoraFim));

            if (bloqueadoPorExcecao)
                throw new ArgumentException("Indisponivel devido a excecao na agenda do medico.");

            var overlap = await _db.Consultas
                .AsNoTracking()
                .AnyAsync(c => c.MedicoId == nova.MedicoId &&
                               c.Estado != "Cancelada" &&
                               c.DataConsulta < end &&
                               c.DataConsulta.AddMinutes(c.Duracao) > start, ct);

            if (overlap)
                throw new ArgumentException("O medico ja tem uma consulta nesse horario.");

            var salaLivreId = await _db.Salas
                .Where(s => !_db.Consultas.Any(c =>
                    c.SalaId == s.Id &&
                    c.Estado != "Cancelada" &&
                    c.DataConsulta < end &&
                    c.DataConsulta.AddMinutes(c.Duracao) > start))
                .Select(s => s.Id)
                .OrderBy(id => id)
                .FirstOrDefaultAsync(ct);

            if (salaLivreId == 0)
                throw new ArgumentException("Nao ha salas disponiveis nesse horario.");

            nova.SalaId = salaLivreId;

            nova.Duracao = 30;
            nova.Estado = "Confirmada";

            await _consultas.AddAsync(nova);
            return nova;
        }

        public async Task<bool> CancelByPacienteAsync(int consultaId, int pacienteId, CancellationToken ct = default)
        {
            var c = await _consultas.GetByIdAsync(consultaId);
            if (c is null) return false;

            if (c.PacienteId != pacienteId)
                throw new ArgumentException("Consulta nao pertence a este paciente.");

            if (c.Estado == "Cancelada") return true; 

            c.Estado = "Cancelada";
            await _consultas.UpdateAsync(c);

            var descricao = $"CANCELAMENTO_PACIENTE|consulta:{c.Id}|inicio:{c.DataConsulta:O}|paciente:{pacienteId}";
            if (!await _notificacoes.ExistsAsync("Cancelamento", descricao, c.MedicoId, null))
            {
                await _notificacoes.AddAsync(new Notificacao
                {
                    Categoria = "Cancelamento",
                    Descricao = descricao,
                    MedicoId = c.MedicoId,
                    PacienteId = null
                });
            }

            return true;
        }

        public async Task<bool> CancelByMedicoAsync(int consultaId, int medicoId, CancellationToken ct = default)
        {
            var c = await _consultas.GetByIdAsync(consultaId);
            if (c is null) return false;

            if (c.MedicoId != medicoId)
                throw new ArgumentException("Consulta nao pertence a este medico.");

            if (c.Estado == "Cancelada") return true; 

            c.Estado = "Cancelada";
            await _consultas.UpdateAsync(c);

            var descricao = $"CANCELAMENTO_MEDICO|consulta:{c.Id}|inicio:{c.DataConsulta:O}|medico:{medicoId}";
            if (!await _notificacoes.ExistsAsync("Cancelamento", descricao, null, c.PacienteId))
            {
                await _notificacoes.AddAsync(new Notificacao
                {
                    Categoria = "Cancelamento",
                    Descricao = descricao,
                    MedicoId = null,
                    PacienteId = c.PacienteId
                });
            }

            return true;
        }

        public async Task<bool> MarkLateByMedicoAsync(int consultaId, int medicoId, CancellationToken ct = default)
        {
            var c = await _consultas.GetByIdAsync(consultaId);
            if (c is null) return false;
            if (c.MedicoId != medicoId)
                throw new ArgumentException("Consulta nao pertence a este medico.");

            var start = c.DataConsulta;
            var endOfDay = new DateTime(start.Year, start.Month, start.Day, 23, 59, 59, DateTimeKind.Utc);

            var proximas = await _consultas.GetByMedicoRangeAsync(medicoId, start, endOfDay, true, ct);

            foreach (var x in proximas)
            {
                var desc = $"ATRASO_MEDICO|consulta:{x.Id}|inicio:{x.DataConsulta:O}|medico:{medicoId}";
                var exists = await _notificacoes.ExistsAsync("AtrasoMedico", desc, null, x.PacienteId);
                if (!exists)
                {
                    await _notificacoes.AddAsync(new Notificacao
                    {
                        Categoria = "AtrasoMedico",
                        Descricao = desc,
                        PacienteId = x.PacienteId,
                        MedicoId = null
                    });
                }
            }

            return true;
        }

        public async Task<bool> MarkLateByPacienteAsync(int consultaId, int pacienteId, CancellationToken ct = default)
        {
            var c = await _consultas.GetByIdAsync(consultaId);
            if (c is null) return false;
            if (c.PacienteId != pacienteId)
                throw new ArgumentException("Consulta nao pertence a este paciente.");

            var desc = $"ATRASO_PACIENTE|consulta:{c.Id}|inicio:{c.DataConsulta:O}|paciente:{pacienteId}";
            var exists = await _notificacoes.ExistsAsync("AtrasoPaciente", desc, c.MedicoId, null);
            if (!exists)
            {
                await _notificacoes.AddAsync(new Notificacao
                {
                    Categoria = "AtrasoPaciente",
                    Descricao = desc,
                    MedicoId = c.MedicoId,
                    PacienteId = null
                });
            }

            return true;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _consultas.GetByIdAsync(id);
            if (existing is null) throw new KeyNotFoundException($"Consulta {id} nao existe.");
            await _consultas.DeleteAsync(id);
        }
    }
}
