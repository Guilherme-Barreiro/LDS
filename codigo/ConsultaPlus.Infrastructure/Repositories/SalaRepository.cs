using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ConsultaPlus.Infrastructure.Repositories
{
    public class SalaRepository : GenericRepository<Sala>, ISalaRepository
    {
        public SalaRepository(ApplicationDbContext context) : base(context) { }
    }
}