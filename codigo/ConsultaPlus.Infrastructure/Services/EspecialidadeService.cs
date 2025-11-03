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

    public Task<IEnumerable<Especialidade>> GetByNameAsync(string name) => _especialidadeRepository.GetByNameAsync(name);


    public async Task<int> AddAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome inválido.", nameof(name));

        var nomeTrim = name.Trim();

        var todas = await _especialidadeRepository.GetAllAsync();
        if (todas.Any(e => e.Nome.Equals(nomeTrim, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe uma especialidade com esse nome.");

        return await _especialidadeRepository.AddAsync(name);
    }

    public async Task UpdateAsync(int id, string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome inválido.", nameof(novoNome));

        var esp = await _especialidadeRepository.GetByIdAsync(id);
        if (esp == null)
            throw new KeyNotFoundException("Especialidade não encontrada.");

        var novoTrim = novoNome.Trim();

        var todas = await _especialidadeRepository.GetAllAsync();
        if (todas.Any(e => e.Nome.Equals(novoTrim, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe uma especialidade com esse nome.");

        await _especialidadeRepository.UpdateAsync(id, novoTrim);
    }

    public async Task DeleteAsync(int id)
    {
        var ent = await _especialidadeRepository.GetByIdAsync(id);
        if (ent == null) throw new KeyNotFoundException("Especialidade não encontrada.");

        await _especialidadeRepository.DeleteAsync(id);
    }
}