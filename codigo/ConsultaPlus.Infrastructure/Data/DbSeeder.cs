using System.Linq;
using System.Threading.Tasks;
using ConsultaPlus.Core.Models;

namespace ConsultaPlus.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(ApplicationDbContext db)
        {
            if (!db.Admins.Any())
            {
                db.Admins.Add(new Admin
                {
                    Username = "admin",
                    Email = "admin@sistema",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin")
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
