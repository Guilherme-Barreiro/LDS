using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.UoW
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public EfUnitOfWork(ApplicationDbContext db) => _db = db;

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }
}
