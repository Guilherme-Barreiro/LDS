using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Infrastructure.Services
{
    public class SalasService : ISalasService
    {
        private readonly ISalaRepository _repo;
        private readonly IUnitOfWork _uow;

        public SalasService(ISalaRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public Task<IEnumerable<Sala>> GetAllAsync() => _repo.GetAllAsync();

        public Task<Sala?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public Task<IEnumerable<Sala>> SearchAsync(string nome) => _repo.SearchByNameAsync(nome);

        public async Task<int> CreateAsync(string nome)
        {
            var n = (nome ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n))
                throw new ArgumentException("Nome da sala é obrigatório.");

            // Regra: nome único (case-insensitive)
            if (await _repo.ExistsByNameAsync(n))
                throw new InvalidOperationException("Já existe uma sala com esse nome.");

            var sala = new Sala { Nome = n };
            await _repo.AddAsync(sala);
            await _uow.SaveChangesAsync();
            return sala.Id;
        }

        public async Task DeleteAsync(int id)
        {
            var sala = await _repo.GetByIdAsync(id);
            if (sala is null)
                throw new KeyNotFoundException("Sala não existe.");

            // Regra: não apagar se tiver consultas futuras
            if (await _repo.HasFutureConsultasAsync(id, DateTime.UtcNow))
                throw new InvalidOperationException("Não é possível remover: a sala tem consultas futuras.");

            await _repo.DeleteAsync(id);
            await _uow.SaveChangesAsync();
        }
    }
}
