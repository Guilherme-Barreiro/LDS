using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsultaPlus.API.Controllers;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static ConsultaPlus.API.Controllers.AdminsController;

namespace ConsultaPlus.Tests.Controllers
{
    public class AdminsControllerTests
    {
        private static AdminsController CreateController(Mock<IAdminRepository> repoMock, ClaimsPrincipal? user = null)
        {
            var controller = new AdminsController(repoMock.Object);

            var httpContext = new DefaultHttpContext();
            if (user != null)
                httpContext.User = user;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Fact]
        public async Task SeedFirst_WhenNoAdminExists_CreatesAdminAndReturnsOk()
        {
            var repo = new Mock<IAdminRepository>();
            repo.Setup(r => r.AnyAsync()).ReturnsAsync(false);

            Admin? saved = null;
            repo.Setup(r => r.AddAsync(It.IsAny<Admin>()))
                .Returns(Task.CompletedTask)
                .Callback<Admin>(a => saved = a);

            var controller = CreateController(repo);
            var dto = new CreateAdminDto(" admin ", "Teste1234.", " admin@local ");

            var result = await controller.SeedFirst(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = ok.Value!;
            var message = payload.GetType().GetProperty("message")!.GetValue(payload) as string;
            Assert.Equal("Administrador inicial criado.", message);

            repo.Verify(r => r.AnyAsync(), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<Admin>()), Times.Once);

            Assert.NotNull(saved);
            Assert.Equal("admin", saved!.Username); 
            Assert.Equal("admin@local", saved.Email); 
            Assert.False(string.IsNullOrWhiteSpace(saved.PasswordHash));
            Assert.NotEqual("Teste1234.", saved.PasswordHash); 
        }

        [Fact]
        public async Task SeedFirst_WhenAdminAlreadyExists_ReturnsBadRequest_AndDoesNotCreate()
        {
            var repo = new Mock<IAdminRepository>();
            repo.Setup(r => r.AnyAsync()).ReturnsAsync(true);

            var controller = CreateController(repo);
            var dto = new CreateAdminDto("admin", "Teste1234.", "admin@local");

            var result = await controller.SeedFirst(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var payload = bad.Value!;
            var message = payload.GetType().GetProperty("message")!.GetValue(payload) as string;
            Assert.Equal("Já existe um administrador.", message);

            repo.Verify(r => r.AddAsync(It.IsAny<Admin>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenUserHasAdminRole_CreatesAndReturnsOk()
        {
            var repo = new Mock<IAdminRepository>();
            repo.Setup(r => r.AddAsync(It.IsAny<Admin>())).Returns(Task.CompletedTask);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth");

            var user = new ClaimsPrincipal(identity);
            var controller = CreateController(repo, user);

            var dto = new CreateAdminDto("admin2", "OutraSenha1!", "admin2@local");

            var result = await controller.Create(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = ok.Value!;
            var message = payload.GetType().GetProperty("message")!.GetValue(payload) as string;
            Assert.Equal("Administrador criado.", message);

            repo.Verify(r => r.AddAsync(It.IsAny<Admin>()), Times.Once);
        }

        [Fact]
        public void Create_HasAuthorizeAttributeWithAdminRole()
        {
            var method = typeof(AdminsController).GetMethod(nameof(AdminsController.Create));
            Assert.NotNull(method);

            var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                              .Cast<AuthorizeAttribute>()
                              .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("Admin", attr!.Roles);
        }
    }
}
