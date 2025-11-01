using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ConsultaPlus.API.Controllers;

namespace ConsultaPlus.Tests.Auth
{
    public class TestControllerUnitTests
    {
        [Fact]
        public void Publico_DeveRetornarOk_ComMensagem()
        {
            var controller = new TestController();

            var result = controller.GetDadosPublicos();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<string>(ok.Value);
            Assert.Contains("informação pública", body);
        }

        [Fact]
        public void Protegido_ComClaims_DeveRetornarOk_ComDadosDoUtilizador()
        {
            var controller = new TestController();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, "user@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
            };
            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var result = controller.GetDadosProtegidos();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<string>(ok.Value);
            Assert.Contains("user-123", body);
            Assert.Contains("user@example.com", body);
            Assert.Contains("Admin", body);
        }
    }
}
