using System.Net;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Core.Models;                 
using ConsultaPlus.API.DTOs.Consultas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;

namespace ConsultaPlus.Tests.Integracao.Consultas
{
    [Collection("Integration")]
    public class ConsultasControllerTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public ConsultasControllerTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private sealed record Ids(int PacienteId, int MedicoId, int SalaId, int EspecialidadeId);

        private async Task<Ids> SeedBaseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Consultas.RemoveRange(db.Consultas);
            db.HorariosExcecaoMedicos.RemoveRange(db.HorariosExcecaoMedicos);
            db.HorariosTrabalhoMedicos.RemoveRange(db.HorariosTrabalhoMedicos);
            db.EspecialidadesMedico.RemoveRange(db.EspecialidadesMedico);
            db.Medicos.RemoveRange(db.Medicos);
            db.Pacientes.RemoveRange(db.Pacientes);
            db.Salas.RemoveRange(db.Salas);
            db.Especialidades.RemoveRange(db.Especialidades);
            await db.SaveChangesAsync();

            var med = new Core.Models.Medico
            {
                NomeCompleto = "Dr IT",
                Email = "dr@it",
                Telemovel = "900000000",
                NUtente = Guid.NewGuid().ToString("N")[..12],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-40)
            };
            var pac = new Core.Models.Paciente
            {
                NomeCompleto = "Pac IT",
                Email = "pac@it",
                Telemovel = "911111111",
                NUtente = Guid.NewGuid().ToString("N")[..12],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-20)
            };
            var esp = new Core.Models.Especialidade { Nome = "Cardio" };
            var sala = new Core.Models.Sala { Nome = "Sala 1" };

            db.Medicos.Add(med);
            db.Pacientes.Add(pac);
            db.Especialidades.Add(esp);
            db.Salas.Add(sala);
            await db.SaveChangesAsync();

            db.EspecialidadesMedico.Add(new Core.Models.EspecialidadeMedico { MedicoId = med.Id, EspecialidadeId = esp.Id });

            var dias = new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sab", "Dom" };
            foreach (var d in dias)
            {
                db.HorariosTrabalhoMedicos.Add(new Core.Models.HorarioTrabalhoMedico
                {
                    MedicoId = med.Id,
                    DiaSemana = d,
                    HoraInicio = TimeSpan.FromHours(8),
                    HoraFim = TimeSpan.FromHours(18)
                });
            }

            await db.SaveChangesAsync();

            return new Ids(pac.Id, med.Id, sala.Id, esp.Id);
        }

        private static DateTime Slot(DateTime utc, int hour, int minute = 0)
        {
            var d = utc.Date.AddHours(hour).AddMinutes(minute);
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        private async Task<ConsultaResponseDto> PostConsultaAsync(Ids ids, DateTime inicioUtc)
        {
            var body = new
            {
                pacienteId = ids.PacienteId,
                medicoId = ids.MedicoId,
                especialidadeId = ids.EspecialidadeId,
                dataConsulta = inicioUtc
            };

            var resp = await _client.PostAsJsonAsync("/api/Consultas", body);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            var dto = await resp.Content.ReadFromJsonAsync<ConsultaResponseDto>();
            Assert.NotNull(dto);
            return dto!;
        }

        [Fact]
        public async Task Post__201_ComBody_EstadoConfirmada()
        {
            var ids = await SeedBaseAsync();
            var inicio = new DateTime(2025, 12, 04, 10, 0, 0, DateTimeKind.Utc);

            var body = new
            {
                pacienteId = ids.PacienteId,
                medicoId = ids.MedicoId,
                especialidadeId = ids.EspecialidadeId,
                dataConsulta = inicio
            };

            var resp = await _client.PostAsJsonAsync("/api/Consultas", body);

            if (resp.StatusCode != HttpStatusCode.Created)
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Expected Created, but got {resp.StatusCode}. Error: {errorContent}");
            }

            var dto = await resp.Content.ReadFromJsonAsync<ConsultaResponseDto>();
            Assert.NotNull(dto);

            Assert.True(dto!.Id > 0);
            Assert.Equal(ids.PacienteId, dto.PacienteId);
            Assert.Equal(ids.MedicoId, dto.MedicoId);
            Assert.Equal(ids.SalaId, dto.SalaId);
            Assert.Equal(ids.EspecialidadeId, dto.EspecialidadeId);
            Assert.Equal(inicio, dto.DataConsulta);
            Assert.Equal(30, dto.Duracao);
            Assert.Equal("Confirmada", dto.Estado);
        }

        [Fact]
        public async Task GetById__200_AposCriar()
        {
            var ids = await SeedBaseAsync();
            var created = await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 10));

            var resp = await _client.GetAsync($"/api/Consultas/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var dto = await resp.Content.ReadFromJsonAsync<ConsultaResponseDto>();
            Assert.NotNull(dto);
            Assert.Equal(created.Id, dto!.Id);
        }

        [Fact]
        public async Task GetAll__200_ContemRegistos()
        {
            var ids = await SeedBaseAsync();
            await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 11));
            await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 12));

            var resp = await _client.GetAsync("/api/Consultas");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<ConsultaResponseDto>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 2);
        }

        [Fact]
        public async Task GetByMedico__FiltraPorMedico()
        {
            var ids = await SeedBaseAsync();

            await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 11));

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var outro = new Core.Models.Medico
                {
                    NomeCompleto = "Dr B",
                    Email = "b@x",
                    Telemovel = "900000001",
                    NUtente = Guid.NewGuid().ToString("N")[..12],
                    PasswordHash = "x",
                    DataNascimento = DateTime.UtcNow.AddYears(-45)
                };
                db.Medicos.Add(outro);
                await db.SaveChangesAsync();

                db.Consultas.Add(new Consulta
                {
                    PacienteId = ids.PacienteId,
                    MedicoId = outro.Id,
                    SalaId = ids.SalaId,
                    EspecialidadeId = ids.EspecialidadeId,
                    DataConsulta = Slot(DateTime.UtcNow.AddDays(1), 12),
                    Duracao = 30,
                    Estado = "Confirmada"
                });
                await db.SaveChangesAsync();
            }

            var resp = await _client.GetAsync($"/api/Consultas/medico/{ids.MedicoId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<ConsultaResponseDto>>();
            Assert.NotNull(list);
            Assert.All(list!, c => Assert.Equal(ids.MedicoId, c.MedicoId));
        }

        [Fact]
        public async Task GetByPaciente__FiltraPorPaciente()
        {
            var ids = await SeedBaseAsync();

            await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 11));
            await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 12));

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var outro = new Paciente
                {
                    NomeCompleto = "Pac B",
                    Email = "pb@x",
                    Telemovel = "922222222",
                    NUtente = Guid.NewGuid().ToString("N")[..12],
                    PasswordHash = "x",
                    DataNascimento = DateTime.UtcNow.AddYears(-30)
                };
                db.Pacientes.Add(outro);
                await db.SaveChangesAsync();

                db.Consultas.Add(new Consulta
                {
                    PacienteId = outro.Id,
                    MedicoId = ids.MedicoId,
                    SalaId = ids.SalaId,
                    EspecialidadeId = ids.EspecialidadeId,
                    DataConsulta = Slot(DateTime.UtcNow.AddDays(1), 13),
                    Duracao = 30,
                    Estado = "Confirmada"
                });
                await db.SaveChangesAsync();
            }

            var resp = await _client.GetAsync($"/api/Consultas/paciente/{ids.PacienteId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<ConsultaResponseDto>>();
            Assert.NotNull(list);
            Assert.All(list!, c => Assert.Equal(ids.PacienteId, c.PacienteId));
        }

        [Fact]
        public async Task Delete__204_E_AposIsso_GetById_404()
        {
            var ids = await SeedBaseAsync();
            var created = await PostConsultaAsync(ids, Slot(DateTime.UtcNow.AddDays(1), 11));

            var del = await _client.DeleteAsync($"/api/Consultas/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var nf = await _client.GetAsync($"/api/Consultas/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
        }
    }
}
