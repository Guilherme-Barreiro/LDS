using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Infrastructure.Services
{
    public class ConsultaService : IConsultaService
    {
        private readonly IConsultaRepository _consultas;
        private readonly IMedicoRepository _medicos;
        private readonly IPacienteRepository _pacientes;
        private readonly ISalaRepository _salas;
        private readonly IEspecialidadeService _especialidades;

        public ConsultaService(
            IConsultaRepository consultas,
            IMedicoRepository medicos,
            IPacienteRepository pacientes,
            ISalaRepository salas,
            IEspecialidadeService especialidades)
        {
            _consultas = consultas;
            _medicos = medicos;
            _pacientes = pacientes;
            _salas = salas;
            _especialidades = especialidades;
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
            if (await _pacientes.GetByIdAsync(nova.PacienteId) is null)
                throw new ArgumentException($"PacienteId {nova.PacienteId} não existe.");
            if (await _medicos.GetByIdAsync(nova.MedicoId) is null)
                throw new ArgumentException($"MedicoId {nova.MedicoId} não existe.");
            if (await _salas.GetByIdAsync(nova.SalaId) is null)
                throw new ArgumentException($"SalaId {nova.SalaId} não existe.");
            if (await _especialidades.GetByIdAsync(nova.EspecialidadeId) is null)
                throw new ArgumentException($"EspecialidadeId {nova.EspecialidadeId} não existe.");

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
