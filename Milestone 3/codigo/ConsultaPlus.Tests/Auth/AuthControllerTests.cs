using System;
using System.Threading.Tasks;
using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs;
using ConsultaPlus.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ConsultaPlus.Tests.Auth
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _auth = new();

        private AuthController CreateController()
        {
            var ctrl = new AuthController(_auth.Object);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return ctrl;
        }

        [Fact]
        public async Task Login_Ok_Returns_Token()
        {
            _auth.Setup(a => a.LoginAsync("999", "1234")).ReturnsAsync("JWT");
            var ctrl = CreateController();

            var res = await ctrl.Login(new LoginDto { NUtente = "999", Password = "1234" }) as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal(200, res!.StatusCode);
            Assert.Contains("Token", res.Value!.ToString());
        }

        [Fact]
        public async Task Login_Invalid_Returns_401()
        {
            _auth.Setup(a => a.LoginAsync("x", "y")).ThrowsAsync(new Exception("bad"));
            var ctrl = CreateController();

            var res = await ctrl.Login(new LoginDto { NUtente = "x", Password = "y" }) as ObjectResult;

            Assert.NotNull(res);
            Assert.Equal(401, res!.StatusCode);
        }

        [Fact]
        public async Task Logout_Passes_Header_To_Service()
        {
            var ctrl = CreateController();
            ctrl.Request.Headers.Authorization = "Bearer ABC";

            var res = await ctrl.Logout() as OkObjectResult;

            _auth.Verify(a => a.LogoutAsync("Bearer ABC"), Times.Once);
            Assert.Equal(200, res!.StatusCode);
        }

        [Fact]
        public async Task Forgot_Returns_ResetToken_In_Ok()
        {
            _auth.Setup(a => a.CreatePasswordResetAsync("123")).ReturnsAsync("RESET");
            var ctrl = CreateController();

            var res = await ctrl.Forgot(new ForgotPasswordDto { Identifier = "123" }) as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal(200, res!.StatusCode);
            Assert.Contains("RESET", res.Value!.ToString());
        }

        [Fact]
        public async Task Reset_Ok_Returns_200()
        {
            _auth.Setup(a => a.ResetPasswordAsync("T", "P")).Returns(Task.CompletedTask);
            var ctrl = CreateController();

            var res = await ctrl.Reset(new ResetPasswordDto { Token = "T", NewPassword = "P" }) as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal(200, res!.StatusCode);
        }

        [Fact]
        public async Task Reset_BadRequest_On_Exception()
        {
            _auth.Setup(a => a.ResetPasswordAsync("bad", "P")).ThrowsAsync(new Exception("x"));
            var ctrl = CreateController();

            var res = await ctrl.Reset(new ResetPasswordDto { Token = "bad", NewPassword = "P" }) as ObjectResult;

            Assert.NotNull(res);
            Assert.Equal(400, res!.StatusCode);
        }

        [Fact]
        public async Task Forgot_UnknownIdentifier_Returns200_WithEmptyToken()
        {
            var auth = new Mock<IAuthService>();
            auth.Setup(a => a.CreatePasswordResetAsync("nao-existe")).ReturnsAsync(string.Empty);
            var ctrl = new AuthController(auth.Object);

            var res = await ctrl.Forgot(new ForgotPasswordDto { Identifier = "nao-existe" }) as OkObjectResult;

            Assert.NotNull(res);
            Assert.Equal(200, res!.StatusCode);
            var json = System.Text.Json.JsonSerializer.Serialize(res.Value);
            Assert.Contains("\"resetToken\":\"\"", json); 
        }

        [Fact]
        public async Task Forgot_Exception_Returns400()
        {
            var auth = new Mock<IAuthService>();
            auth.Setup(a => a.CreatePasswordResetAsync("X")).ThrowsAsync(new Exception("boom"));
            var ctrl = new AuthController(auth.Object);

            var res = await ctrl.Forgot(new ForgotPasswordDto { Identifier = "X" }) as ObjectResult;

            Assert.NotNull(res);
            Assert.Equal(400, res!.StatusCode);
        }

    }
}
