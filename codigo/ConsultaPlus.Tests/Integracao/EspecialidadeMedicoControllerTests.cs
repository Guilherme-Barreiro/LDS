using System.Net;
using System.Net.Http.Json;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.EspecialidadeMedico
{
    public class EspecialidadeMedicoControllerIT : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public EspecialidadeMedicoControllerIT(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }
        public record MedicoVm(int Id, string NomeCompleto, string Email, string Telemovel, string NUtente);
        public record EspecialidadeVm(int Id, string Nome);

        private async Task<ApplicationDbContext> GetDbAsync()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        private async Task CleanupAsync()
        {
            var db = await GetDbAsync();
            db.EspecialidadesMedico.RemoveRange(db.EspecialidadesMedico);
            db.Medicos.RemoveRange(db.Medicos);
            db.Especialidades.RemoveRange(db.Especialidades);
            await db.SaveChangesAsync();
        }

        private async Task<int> SeedMedicoAsync(string? nome = null)
        {
            var db = await GetDbAsync();
            var m = new Core.Models.Medico
            {
                NomeCompleto = nome ?? "Dr Test",
                Email = $"{Guid.NewGuid():N}@ex.com",
                Telemovel = "9" + Random.Shared.Next(10000000, 99999999),
                NUtente = Guid.NewGuid().ToString("N")[..12],
                PasswordHash = "x",
                DataNascimento = DateTime.UtcNow.AddYears(-40)
            };
            db.Medicos.Add(m);
            await db.SaveChangesAsync();
            return m.Id;
        }

        private async Task<int> SeedEspecialidadeAsync(string? nome = null)
        {
            var db = await GetDbAsync();
            var e = new Core.Models.Especialidade { Nome = nome ?? $"Esp-{Guid.NewGuid():N}" };
            db.Especialidades.Add(e);
            await db.SaveChangesAsync();
            return e.Id;
        }

        private record AssocDto(int MedicoId, int EspecialidadeId);

        private async Task<HttpResponseMessage> AssocAsync(int medicoId, int especialidadeId)
            => await _client.PostAsJsonAsync("/api/EspecialidadeMedico/add",
                                             new AssocDto(medicoId, especialidadeId));

        private async Task<HttpResponseMessage> UnassocAsync(int medicoId, int especialidadeId)
        {
            var req = new HttpRequestMessage(HttpMethod.Delete,
                "/api/EspecialidadeMedico/delete")
            {
                Content = JsonContent.Create(new AssocDto(medicoId, especialidadeId))
            };
            return await _client.SendAsync(req);
        }

        [Fact]
        public async Task Associar__201_QuandoMedicoEEspecialidadeExistemENaoHaLigacao()
        {
            await CleanupAsync();
            var mid = await SeedMedicoAsync("Dr A");
            var eid = await SeedEspecialidadeAsync("Cardio");

            var res = await AssocAsync(mid, eid);

            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        }

        [Fact]
        public async Task Associar__404_QuandoMedicoInexistente()
        {
            await CleanupAsync();
            var eid = await SeedEspecialidadeAsync();

            var res = await AssocAsync(999999, eid);

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Associar__404_QuandoEspecialidadeInexistente()
        {
            await CleanupAsync();
            var mid = await SeedMedicoAsync();

            var res = await AssocAsync(mid, 999999);

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task Associar__409_QuandoDuplicado()
        {
            await CleanupAsync();
            var mid = await SeedMedicoAsync("Dr Dup");
            var eid = await SeedEspecialidadeAsync("Dermato");

            var first = await AssocAsync(mid, eid);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var dup = await AssocAsync(mid, eid);
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        }

        [Fact]
        public async Task Remover__204_QuandoExiste_E_Depois404_QuandoJaNaoExiste()
        {
            await CleanupAsync();
            var mid = await SeedMedicoAsync("Dr Rm");
            var eid = await SeedEspecialidadeAsync("ORL");

            var ok = await AssocAsync(mid, eid);
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

            var del = await UnassocAsync(mid, eid);
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var delAgain = await UnassocAsync(mid, eid);
            Assert.Equal(HttpStatusCode.NotFound, delAgain.StatusCode);
        }

        [Fact]
        public async Task GetMedicosPorEspecialidade__200_ComLista_E_404_QuandoVazio()
        {
            await CleanupAsync();
            var m1 = await SeedMedicoAsync("Dr 1");
            var m2 = await SeedMedicoAsync("Dr 2");
            var espe = await SeedEspecialidadeAsync("Cardio");
            var outra = await SeedEspecialidadeAsync("Neuro");

            Assert.Equal(HttpStatusCode.Created, (await AssocAsync(m1, espe)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await AssocAsync(m2, espe)).StatusCode);

            var ok = await _client.GetAsync($"/api/EspecialidadeMedico/medicos-por-especialidade/{espe}");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

            var medicos = await ok.Content.ReadFromJsonAsync<List<MedicoVm>>();
            Assert.NotNull(medicos);
            Assert.Contains(medicos!, x => x.NomeCompleto == "Dr 1");
            Assert.Contains(medicos!, x => x.NomeCompleto == "Dr 2");

            var nf = await _client.GetAsync($"/api/EspecialidadeMedico/medicos-por-especialidade/{outra}");
            Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
        }

        [Fact]
        public async Task GetEspecialidadesPorMedico__200_ComLista_E_404_QuandoVazio()
        {
            await CleanupAsync();
            var mid = await SeedMedicoAsync("Dr X");
            var e1 = await SeedEspecialidadeAsync("Onco");
            var e2 = await SeedEspecialidadeAsync("Pedia");
            var outroMed = await SeedMedicoAsync("Dr Y");

            Assert.Equal(HttpStatusCode.Created, (await AssocAsync(mid, e1)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await AssocAsync(mid, e2)).StatusCode);

            var ok = await _client.GetAsync($"/api/EspecialidadeMedico/especialidades-por-medico/{mid}");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

            var especialidades = await ok.Content.ReadFromJsonAsync<List<EspecialidadeVm>>();
            Assert.NotNull(especialidades);
            Assert.Contains(especialidades!, x => x.Nome == "Onco");
            Assert.Contains(especialidades!, x => x.Nome == "Pedia");

            var nf = await _client.GetAsync($"/api/EspecialidadeMedico/especialidades-por-medico/{outroMed}");
            Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);
        }
    }
}
