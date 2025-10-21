using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Infrastructure
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // A "Opção Nuclear": Colamos a connection string diretamente aqui.
            // O código já não precisa de procurar pelo appsettings.json.
            var connectionString = "Server=localhost,1433;Database=ConsultaPlusDB;User Id=sa;Password=Teste1234.;TrustServerCertificate=True";

            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}