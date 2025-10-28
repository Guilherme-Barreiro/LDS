using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class ConsultaRepository : GenericRepository<Consulta>, IConsultaRepository
    {
        public ConsultaRepository(ApplicationDbContext context) : base(context) { }
    }
}
