using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class EspecialidadeRepository : GenericRepository<Especialidade>, IEspecialidadeRepository
    {
        public EspecialidadeRepository(ApplicationDbContext context) : base(context) { }
    }
}
