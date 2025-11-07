using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using System;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Services
{
    public class EspecialidadeMedicoService : IEspecialidadeMedicoService
    {
        private readonly IEspecialidadeMedicoRepository _especialidadeMedicoRepository;
        private readonly IEspecialidadeRepository _especialidadeRepository;
        private readonly IMedicoRepository _medicoRepository;
        private readonly IUnitOfWork _uow;

        public EspecialidadeMedicoService(IEspecialidadeMedicoRepository especialidadeMedicoRepository,
                                          IEspecialidadeRepository especialidadeRepository,
                                          IMedicoRepository medicoRepository,
                                          IUnitOfWork uow)
        {
            _especialidadeMedicoRepository = especialidadeMedicoRepository;
            _especialidadeRepository = especialidadeRepository;
            _medicoRepository = medicoRepository;
            _uow = uow;
        }

        public Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId)
            => _especialidadeMedicoRepository.GetMedicosByEspecialidadeIdAsync(especialidadeId);

        public Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId)
            => _especialidadeMedicoRepository.GetEspecialidadesByMedicoIdAsync(medicoId);

        public async Task AddAsync(int medicoId, int especialidadeId)
        {
            if (!await _medicoRepository.ExistsAsync(medicoId))
                throw new KeyNotFoundException("Medico nao encontrado.");

            if (await _especialidadeRepository.GetByIdAsync(especialidadeId) == null)
                throw new KeyNotFoundException("Especialidade nao encontrada.");

            if (await _especialidadeMedicoRepository.ExistsAsync(medicoId, especialidadeId))
                throw new InvalidOperationException("Associacao ja existe.");

            var assoc = new EspecialidadeMedico { MedicoId = medicoId, EspecialidadeId = especialidadeId };
            await _especialidadeMedicoRepository.AddAsync(assoc);
            await _uow.SaveChangesAsync();
        }

        public async Task DeleteAsync(int medicoId, int especialidadeId)
        {
            if (!await _especialidadeMedicoRepository.ExistsAsync(medicoId, especialidadeId))
                throw new KeyNotFoundException("Associacao nao encontrada.");
            await _especialidadeMedicoRepository.DeleteAsync(medicoId, especialidadeId);
            await _uow.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(int medicoId, int especialidadeId) => _especialidadeMedicoRepository.ExistsAsync(medicoId, especialidadeId);
    }
}