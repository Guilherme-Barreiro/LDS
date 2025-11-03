using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Infrastructure.Services 
{
    public interface IEspecialidadesService
    {
        Task<IEnumerable<Especialidade>> GetAllAsync();
        Task<Especialidade?> GetByIdAsync(int id);
        Task<int> CreateAsync(string nome);
        Task UpdateAsync(int id, string novoNome);
        Task DeleteAsync(int id);
        Task<IEnumerable<Especialidade>> SearchAsync(string nome);
    }

    public class EspecialidadesService : IEspecialidadesService
    {
        private readonly IEspecialidadeCRUD _repo;
        private readonly IUnitOfWork _uow;

        public EspecialidadesService(IEspecialidadeCRUD repo, IUnitOfWork uow)
        {
            _repo = repo; _uow = uow;
        }

        public Task<IEnumerable<Especialidade>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Especialidade?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public async Task<int> CreateAsync(string nome)
        {
            var n = (nome ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n))
                throw new ArgumentException("Nome obrigatorio.");
            if (await _repo.ExistsByNameAsync(n))
                throw new InvalidOperationException("Ja existe uma especialidade com esse nome.");

            var esp = new Especialidade { Nome = n };
            await _repo.AddAsync(esp);
            await _uow.SaveChangesAsync();
            return esp.Id;
        }

        public async Task UpdateAsync(int id, string novoNome)
        {
            var e = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Especialidade nao existe.");
            var n = (novoNome ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n))
                throw new ArgumentException("Nome obrigatorio.");
            if (!string.Equals(e.Nome, n, StringComparison.OrdinalIgnoreCase)
                && await _repo.ExistsByNameAsync(n))
                throw new InvalidOperationException("Ja existe uma especialidade com esse nome.");

            e.Nome = n;
            await _repo.UpdateAsync(e);
            await _uow.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Especialidade nao existe.");
            if (await _repo.HasMedicosAsync(id))
                throw new InvalidOperationException("Nao e possível remover: existem medicos associados.");

            await _repo.DeleteAsync(id);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Especialidade>> SearchAsync(string nome)
        {
            var all = await _repo.GetAllAsync();
            var n = (nome ?? "").Trim();
            return all.Where(e => !string.IsNullOrEmpty(e.Nome)
                               && e.Nome.Contains(n, StringComparison.OrdinalIgnoreCase));
        }
    }
}
