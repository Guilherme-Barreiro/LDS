using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using System;
using System.Threading.Tasks;

public class EspecialidadeService : IEspecialidadeCRUD
{
    private readonly IEspecialidadeCRUD _especialidadeRepository;

    public EspecialidadeService(IEspecialidadeCRUD especialidadeRepository)
    {
        _especialidadeRepository = especialidadeRepository;
    }

    public Task<IEnumerable<Especialidade>> GetAllAsync() => _especialidadeRepository.GetAllAsync();

    public Task<Especialidade?> GetByIdAsync(int id) => _especialidadeRepository.GetByIdAsync(id);

    public async Task AddAsync(Especialidade especialidade)
    {
        if (string.IsNullOrWhiteSpace(especialidade.Nome))
            throw new ArgumentException("Nome inválido.", nameof(especialidade.Nome));

        var nomeTrim = especialidade.Nome.Trim();

        var todas = await _especialidadeRepository.GetAllAsync();
        if (todas.Any(e => e.Nome.Equals(nomeTrim, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe uma especialidade com esse nome.");

        await _especialidadeRepository.AddAsync(especialidade);
    }

    public async Task UpdateAsync(Especialidade especialidade, string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome inválido.", nameof(novoNome));

        var esp = await _especialidadeRepository.GetByIdAsync(especialidade.Id);
        if (esp == null)
            throw new KeyNotFoundException("Especialidade não encontrada.");

        var novoTrim = novoNome.Trim();

        var todas = await _especialidadeRepository.GetAllAsync();
        if (todas.Any(e => e.Nome.Equals(novoTrim, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe uma especialidade com esse nome.");

        await _especialidadeRepository.UpdateAsync(especialidade, novoTrim);
    }

    public async Task DeleteAsync(int id)
    {
        var ent = await _especialidadeRepository.GetByIdAsync(id);
        if (ent == null) throw new KeyNotFoundException("Especialidade não encontrada.");

        await _especialidadeRepository.DeleteAsync(id);
    }
}