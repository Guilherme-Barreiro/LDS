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
        private readonly ApplicationDbContext _db; // << ADICIONADO

        public ConsultaService(
            IConsultaRepository consultas,
            IMedicoRepository medicos,
            IPacienteRepository pacientes,
            ISalaRepository salas,
            IEspecialidadeService especialidades,
            ApplicationDbContext db) // << ADICIONADO
        {
            _consultas = consultas;
            _medicos = medicos;
            _pacientes = pacientes;
            _salas = salas;
            _especialidades = especialidades;
            _db = db; // << ADICIONADO
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
            // 1) Entidades existem
            if (await _pacientes.GetByIdAsync(nova.PacienteId) is null)
                throw new ArgumentException($"PacienteId {nova.PacienteId} não existe.");

            if (await _medicos.GetByIdAsync(nova.MedicoId) is null)
                throw new ArgumentException($"MedicoId {nova.MedicoId} não existe.");

            if (await _especialidades.GetByIdAsync(nova.EspecialidadeId) is null)
                throw new ArgumentException($"EspecialidadeId {nova.EspecialidadeId} não existe.");

            // (Se estiveres a usar Sala agora, mantém; se não, remove estas 2 linhas)
            if (nova.SalaId != 0 && await _salas.GetByIdAsync(nova.SalaId) is null)
                throw new ArgumentException($"SalaId {nova.SalaId} não existe.");

            // 2) VALIDACAO NOVA: Médico tem a especialidade pedida
            var medicoTemEspecialidade = await _db.EspecialidadesMedico
                .AsNoTracking()
                .AnyAsync(em => em.MedicoId == nova.MedicoId && em.EspecialidadeId == nova.EspecialidadeId, ct);

            if (!medicoTemEspecialidade)
                throw new ArgumentException("O médico não possui a especialidade selecionada.");

            // 3) Duração fixa de 30 min (reforço de regra server-side)
            nova.Duracao = 30;

            // 4) Estado padrão
            nova.Estado = "Confirmada";

            await _consultas.AddAsync(nova);
            return nova;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _consultas.GetByIdAsync(id);
            if (existing is null) throw new KeyNotFoundException($"Consulta {id} não existe.");
            await _consultas.DeleteAsync(id);
        }
    }
}
