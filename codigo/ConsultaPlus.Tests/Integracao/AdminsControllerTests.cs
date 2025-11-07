using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace ConsultaPlus.Tests.Integracao.Auth
{
    public class AdminsControllerTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public AdminsControllerTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SeedFirstAdmin_FirstTime_Ok_SecondTime_BadRequest()
        {
            var first = await _client.PostAsJsonAsync("/api/admins/seed-first-admin",
                new { username = "seedadmin", password = "SeedPwd1!", email = "seed@local" });
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await _client.PostAsJsonAsync("/api/admins/seed-first-admin",
                new { username = "other", password = "OtherPwd1!", email = "other@local" });
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task Create_Admin_Without_Token_Works_With_TestAuth()
        {
            var res = await _client.PostAsJsonAsync("/api/admins", new
            {
                username = "noauth",
                password = "Pwd1!",
                email = "noauth@local"
            });

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        [Fact]
        public async Task Create_Admin_With_Bearer_Token_Still_Works_With_TestAuth_Default()
        {
            var _ = await _client.PostAsJsonAsync("/api/admins/seed-first-admin",
                new { username = "admin", password = "Teste1234.", email = "admin@local" });

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/admins");
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "qualquer.coisa.aqui");
            req.Content = JsonContent.Create(new
            {
                username = "admin2",
                password = "OutraSenha1!",
                email = "admin2@local"
            });

            var res = await _client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }
}
