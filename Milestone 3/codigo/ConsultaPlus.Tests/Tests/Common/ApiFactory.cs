using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ConsultaPlus.Infrastructure.Data;

public class ApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _conn;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));

            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();

            services.AddDbContext<ApplicationDbContext>(opt =>
            {
                opt.UseSqlite(_conn);
                opt.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.AddAuthorization(); 

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
