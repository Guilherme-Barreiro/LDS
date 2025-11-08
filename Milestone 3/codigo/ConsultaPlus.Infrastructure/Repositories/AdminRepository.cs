using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        public AdminRepository(ApplicationDbContext context) => _context = context;

        public Task<Admin?> GetByUsernameAsync(string username) =>
            _context.Admins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Username == username);

        public async Task AddAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
            await _context.SaveChangesAsync();
        }

        public Task<bool> AnyAsync()
        {
            return _context.Admins.AnyAsync();
        }
    }
}
