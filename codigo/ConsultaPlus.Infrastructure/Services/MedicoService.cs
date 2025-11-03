using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Infrastructure.Services
{
    public class MedicoService : IMedicoService
    {
        private readonly IMedicoRepository _repo;

        public MedicoService(IMedicoRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<Medico>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync();

        public Task<Medico?> GetByIdAsync(int id, CancellationToken ct = default)
            => _repo.GetByIdAsync(id);

        public Task<IEnumerable<Medico>> SearchByNomeAsync(string nome, CancellationToken ct = default)
            => _repo.SearchByNameAsync(nome);

        public async Task<Medico> CreateAsync(Medico novo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(novo.NomeCompleto)) throw new ArgumentException("NomeCompleto é obrigatório.");
            if (string.IsNullOrWhiteSpace(novo.Email)) throw new ArgumentException("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(novo.NUtente)) throw new ArgumentException("NUtente é obrigatório.");

            await _repo.AddAsync(novo);
            return novo;
        }

        public async Task UpdateAsync(Medico medico, CancellationToken ct = default)
        {
            var existing = await _repo.GetByIdAsync(medico.Id);
            if (existing is null) throw new KeyNotFoundException($"Médico {medico.Id} não existe.");

            existing.NomeCompleto = string.IsNullOrWhiteSpace(medico.NomeCompleto) ? existing.NomeCompleto : medico.NomeCompleto.Trim();
            existing.Email = string.IsNullOrWhiteSpace(medico.Email) ? existing.Email : medico.Email.Trim();
            existing.Telemovel = string.IsNullOrWhiteSpace(medico.Telemovel) ? existing.Telemovel : medico.Telemovel.Trim();
            existing.DataNascimento = medico.DataNascimento != default ? medico.DataNascimento : existing.DataNascimento;

            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null) throw new KeyNotFoundException($"Médico {id} não existe.");
            await _repo.DeleteAsync(id);
        }
    }
}
