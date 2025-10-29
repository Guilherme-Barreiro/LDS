using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Infrastructure.Repositories;
using ConsultaPlus.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Obtém a connection string do ficheiro appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsEnvironment("Testing"))
{
    // Usado só nos testes (WebApplicationFactory define o ambiente)
    builder.Services.AddDbContext<ApplicationDbContext>(opts =>
        opts.UseInMemoryDatabase("TestingDb"));
}
else
{
    // Desenvolvimento/Produção
    builder.Services.AddDbContext<ApplicationDbContext>(opts =>
        opts.UseSqlServer(connectionString));
}

builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IHorarioTrabalhoMedico, HorarioTrabalhoMedicoService>(); 
builder.Services.AddScoped<IHorarioExcecaoMedico, HorarioExcecaoMedicoService>();   

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//OLA

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
