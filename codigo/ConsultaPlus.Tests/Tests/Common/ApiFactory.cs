using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Diz ao host que estamos em "Testing"
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Auth de teste (Admin)
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            // Seed mínimo
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            if (!db.Medicos.Any())
            {
                db.Medicos.Add(new Medico
                {
                    NomeCompleto = "Dr Teste",
                    Email = "dr@x.com",
                    Telemovel = "900000000",
                    NUtente = "UT-TEST",
                    PasswordHash = "x",
                    DataNascimento = DateTime.UtcNow.AddYears(-40)
                });
                db.SaveChanges();
            }
        });
    }
}
