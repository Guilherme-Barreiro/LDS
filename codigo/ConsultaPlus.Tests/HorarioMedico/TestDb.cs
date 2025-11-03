using System;
using Microsoft.EntityFrameworkCore;
using ConsultaPlus.Infrastructure.Data;

namespace ConsultaPlus.Tests.HorarioMedico
{
    internal static class TestDb
    {
        public static ApplicationDbContext Create()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            return new ApplicationDbContext(opts);
        }
    }
}
