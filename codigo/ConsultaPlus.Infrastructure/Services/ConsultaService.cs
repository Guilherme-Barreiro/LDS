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
            // Validações de FK
            if (await _pacientes.GetByIdAsync(nova.PacienteId) is null)
                throw new ArgumentException($"PacienteId {nova.PacienteId} não existe.");
            if (await _medicos.GetByIdAsync(nova.MedicoId) is null)
                throw new ArgumentException($"MedicoId {nova.MedicoId} não existe.");
            if (await _especialidades.GetByIdAsync(nova.EspecialidadeId) is null)
                throw new ArgumentException($"EspecialidadeId {nova.EspecialidadeId} não existe.");

            // 1) Forçar duração = 30 min
            nova.Duracao = 30;

            var inicio = nova.DataConsulta;
            var fim = inicio.AddMinutes(30);

            // 2) Escolher automaticamente uma sala livre
            var salas = await _salas.GetAllAsync();
            var todasConsultas = await _consultas.GetAllAsync();

            bool Overlap(DateTime aIni, DateTime aFim, DateTime bIni, DateTime bFim)
                => aIni < bFim && bIni < aFim; // intervalo semiaberto

            var salaLivre = salas.FirstOrDefault(s =>
                !todasConsultas.Any(c =>
                    c.SalaId == s.Id &&
                    c.Estado == "Confirmada" &&
                    Overlap(c.DataConsulta, c.DataConsulta.AddMinutes(c.Duracao), inicio, fim)));

            if (salaLivre is null)
                throw new InvalidOperationException("Não há nenhuma sala livre para esse horário.");

            nova.SalaId = salaLivre.Id;
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
