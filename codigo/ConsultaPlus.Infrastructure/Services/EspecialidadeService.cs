using ConsultaPlus.API.Repositories;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace ConsultaPlus.Infrastructure.Services
{
    public class EspecialidadeService : IEspecialidadeService
    {
        private readonly IEspecialidadeRepository _especialidadeRepository;
        private readonly IUnitOfWork _uow;

        public EspecialidadeService(IEspecialidadeRepository especialidadeRepository, IUnitOfWork uow)
        {
            _especialidadeRepository = especialidadeRepository;
            _uow = uow;
        }

        public Task<IEnumerable<Especialidade>> GetAllAsync() => _especialidadeRepository.GetAllAsync();

        public Task<Especialidade?> GetByIdAsync(int id) => _especialidadeRepository.GetByIdAsync(id);

        public async Task<int> AddAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome obrigatorio.");

            var nomeTrim = name.Trim();

            if (await _especialidadeRepository.ExistsByNameAsync(nomeTrim))
                throw new InvalidOperationException("Ja existe uma especialidade com esse nome.");


            var esp = new Especialidade { Nome = nomeTrim };
            await _especialidadeRepository.AddAsync(esp);
            await _uow.SaveChangesAsync();
            return esp.Id;
        }

        public async Task UpdateAsync(int id, string novoNome)
        {
            if (string.IsNullOrWhiteSpace(novoNome))
                throw new ArgumentException("Nome obrigatorio.");

            var esp = await _especialidadeRepository.GetByIdAsync(id);
            if (esp == null)
                throw new KeyNotFoundException("Especialidade nao encontrada.");

            var novoTrim = novoNome.Trim();

            if (await _especialidadeRepository.ExistsByNameAndNotIdAsync(novoTrim, id))
                throw new InvalidOperationException("Ja existe uma especialidade com esse nome.");

            esp.Nome = novoTrim;

            await _especialidadeRepository.UpdateAsync(esp);
            await _uow.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var ent = await _especialidadeRepository.GetByIdAsync(id);
            if (ent == null) throw new KeyNotFoundException("Especialidade nao encontrada.");

            if (await _especialidadeRepository.IsLinkedToMedic(id))
                throw new InvalidOperationException("Nao e possivel excluir a especialidade porque existem medicos vinculados.");

            await _especialidadeRepository.DeleteAsync(id);
            await _uow.SaveChangesAsync();
        }
        public async Task<IEnumerable<Especialidade>> SearchAsync(string termo)
        {
            return await _especialidadeRepository.SearchByNameAsync(termo);
        }
    }
}