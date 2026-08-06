
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HRFlow.Infrastructure.Seeding
{
    public static class DepartmentSeeder
    {
        public static async Task SeedDepartmentsAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (!hostEnvironment.IsDevelopment())
            {
                return;
            }

            var dbContext = scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
            var engineeringDepartmentId = Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef");
            if (!dbContext.Set<Department>().Any(d => d.Id == engineeringDepartmentId))
            {
                await dbContext.Set<Department>().AddAsync(new Department { Id = engineeringDepartmentId, Name = "Engineering" });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}