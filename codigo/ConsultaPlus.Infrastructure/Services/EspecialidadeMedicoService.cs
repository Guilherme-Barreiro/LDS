using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using System;
using System.Threading.Tasks;

public class EspecialidadeMedicoService //: IEspecialidadeMedico
{
    /*private readonly IEspecialidadeMedico _especialidadeMedicoRepository;
    private readonly IEspecialidadeCRUD _especialidadeRepository;
    private readonly IMedicoRepository _medicoRepository;

    public EspecialidadeMedicoService(IEspecialidadeMedico especialidadeMedicoRepository,
                                      IEspecialidadeCRUD especialidadeRepository,
                                      IMedicoRepository medicoRepository)
    {
        _especialidadeMedicoRepository = especialidadeMedicoRepository;
        _especialidadeRepository = especialidadeRepository;
        _medicoRepository = medicoRepository;
    }

    public Task<IEnumerable<Medico>> GetMedicosByEspecialidadeIdAsync(int especialidadeId)
        => _especialidadeMedicoRepository.GetMedicosByEspecialidadeIdAsync(especialidadeId);

    public Task<IEnumerable<Especialidade>> GetEspecialidadesByMedicoIdAsync(int medicoId)
        => _especialidadeMedicoRepository.GetEspecialidadesByMedicoIdAsync(medicoId);

    public async Task AssociateAsync(int medicoId, int especialidadeId)
    {
        if (!await _medicoRepository.ExistsAsync(medicoId)) throw new KeyNotFoundException("Médico não encontrado.");
        if (!await _especialidadeRepository.GetByIdAsync(especialidadeId) is Especialidade) throw new KeyNotFoundException("Especialidade não encontrada.");

        if (await _especialidadeMedicoRepository.ExistsAsync(medicoId, especialidadeId))
            throw new InvalidOperationException("Associação já existe.");

        var assoc = new EspecialidadeMedico { MedicoId = medicoId, EspecialidadeId = especialidadeId };
        await _especialidadeMedicoRepository.AddAsync(assoc);
    }

    public async Task RemoveAsync(int medicoId, int especialidadeId)
    {
        if (!await _especialidadeMedicoRepository.ExistsAsync(medicoId, especialidadeId))
            throw new KeyNotFoundException("Associação não encontrada.");
        await _especialidadeMedicoRepository.RemoveAsync(medicoId, especialidadeId);
    }

    public Task<bool> ExistsAsync(int medicoId, int especialidadeId) => _especialidadeMedicoRepository.ExistsAsync(medicoId, especialidadeId);*/
}