using ConsultaPlus.API.Repositories;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Infrastructure.Repositories;
using ConsultaPlus.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsEnvironment("Testing"))
{
    // Usado s� nos testes (WebApplicationFactory define o ambiente)
    builder.Services.AddDbContext<ApplicationDbContext>(opts =>
        opts.UseInMemoryDatabase("TestingDb"));
}
else
{
    // Desenvolvimento/Produ��o
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

builder.Services.AddScoped<IMedicoRepository, MedicoRepository>();
builder.Services.AddScoped<ISalaRepository, SalaRepository>();
builder.Services.AddScoped<IConsultaRepository, ConsultaRepository>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
