using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsultaPlus.Infrastructure.Data;
using ConsultaPlus.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConsultaPlus.Tests.Disponibilidade
{
    public class DisponibilidadeServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly DisponibilidadeService _svc;
        private const int Medico = 7;

        public DisponibilidadeServiceTests()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Disponibilidade-" + Guid.NewGuid())
                .Options;

            _db = new ApplicationDbContext(opts);
            _svc = new DisponibilidadeService(_db);
        }

        public void Dispose() => _db.Dispose();

        private static string PtDay(DateTime d) => d.DayOfWeek switch
        {
            DayOfWeek.Monday => "Seg",
            DayOfWeek.Tuesday => "Ter",
            DayOfWeek.Wednesday => "Qua",
            DayOfWeek.Thursday => "Qui",
            DayOfWeek.Friday => "Sex",
            DayOfWeek.Saturday => "Sab",
            DayOfWeek.Sunday => "Dom",
            _ => "Seg"
        };

        private DateTime Utc(int y, int m, int d, int h, int min = 0)
            => new DateTime(y, m, d, h, min, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetSlotsLivresAsync__IntervaloInvalido__RetornaVazio()
        {
            var from = Utc(2025, 1, 6, 9);   
            var to = Utc(2025, 1, 6, 9);     

            var res = await _svc.GetSlotsLivresAsync(Medico, from, to);

            Assert.Empty(res);
        }

        [Fact]
        public async Task GetSlotsLivresAsync__DentroHorario_E_ArredondaParaProximoSlot()
        {
            var segunda = Utc(2025, 1, 6, 0);
            _db.HorariosTrabalhoMedicos.Add(new Core.Models.HorarioTrabalhoMedico
            {
                MedicoId = Medico,
                DiaSemana = PtDay(segunda),
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(12),
            });
            await _db.SaveChangesAsync();

            var res = await _svc.GetSlotsLivresAsync(
                Medico,
                fromUtc: Utc(2025, 1, 6, 8, 10),
                toUtc: Utc(2025, 1, 6, 12, 00));

            var esperado = new[]
            {
                Utc(2025,1,6,9,0),
                Utc(2025,1,6,9,30),
                Utc(2025,1,6,10,0),
                Utc(2025,1,6,10,30),
                Utc(2025,1,6,11,0),
                Utc(2025,1,6,11,30),
            };

            Assert.Equal(esperado.Length, res.Count);
            Assert.Equal(esperado, res);
        }

        [Fact]
        public async Task GetSlotsLivresAsync__RespeitaExcecoes_E_Consultas()
        {
            var segunda = Utc(2025, 1, 6, 0);
            _db.HorariosTrabalhoMedicos.Add(new Core.Models.HorarioTrabalhoMedico
            {
                MedicoId = Medico,
                DiaSemana = PtDay(segunda),
                HoraInicio = TimeSpan.FromHours(9),
                HoraFim = TimeSpan.FromHours(12),
            });

            _db.HorariosExcecaoMedicos.Add(new Core.Models.HorarioExcecaoMedico
            {
                MedicoId = Medico,
                Data = segunda.Date,
                HoraInicio = TimeSpan.FromHours(10),
                HoraFim = TimeSpan.FromHours(10.5),
                IsReducao = true,
                Motivo = "Reunião"
            });

            _db.Consultas.Add(new Core.Models.Consulta
            {
                MedicoId = Medico,
                PacienteId = 1,
                DataConsulta = Utc(2025, 1, 6, 11, 30),
                Duracao = 30,
                EspecialidadeId = 1,
                Estado = "Confirmada",
                SalaId = 1
            });

            await _db.SaveChangesAsync();

            var res = await _svc.GetSlotsLivresAsync(
                Medico,
                fromUtc: Utc(2025, 1, 6, 9, 0),
                toUtc: Utc(2025, 1, 6, 12, 0));

            var esperado = new[]
            {
                Utc(2025,1,6,9,0),
                Utc(2025,1,6,9,30),
                Utc(2025,1,6,10,30),
                Utc(2025,1,6,11,0),
            };

            Assert.Equal(esperado, res);
        }

        [Fact]
        public async Task GetSlotsLivresAsync__IgnoraSlotsQueAtravessamMeiaNoite()
        {
            var segunda = Utc(2025, 1, 6, 0);
            _db.HorariosTrabalhoMedicos.Add(new Core.Models.HorarioTrabalhoMedico
            {
                MedicoId = Medico,
                DiaSemana = PtDay(segunda),
                HoraInicio = TimeSpan.FromHours(23),
                HoraFim = TimeSpan.FromHours(23.9833) 
            });
            await _db.SaveChangesAsync();

            var res = await _svc.GetSlotsLivresAsync(
                Medico,
                fromUtc: Utc(2025, 1, 6, 23, 25),
                toUtc: Utc(2025, 1, 7, 1, 0));

            Assert.Empty(res);
        }

        [Fact]
        public async Task GetProximosSlotsAsync__RespeitaCount_E_DefaultQuandoNegativo()
        {
            DateTime d = Utc(2025, 1, 6, 0); 
            for (int i = 0; i < 5; i++)
            {
                _db.HorariosTrabalhoMedicos.Add(new Core.Models.HorarioTrabalhoMedico
                {
                    MedicoId = Medico,
                    DiaSemana = PtDay(d.AddDays(i)),
                    HoraInicio = TimeSpan.FromHours(9),
                    HoraFim = TimeSpan.FromHours(17),
                });
            }
            await _db.SaveChangesAsync();

            var top2 = await _svc.GetProximosSlotsAsync(Medico, 2);
            Assert.Equal(2, top2.Count);

            var topDefault = await _svc.GetProximosSlotsAsync(Medico, 0);
            Assert.True(topDefault.Count <= 10);
            Assert.InRange(topDefault.Count, 1, 10);
        }
    }
}
