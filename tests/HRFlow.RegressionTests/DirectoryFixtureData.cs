using HRFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.RegressionTests;

/// <summary>Repeatable synthetic cardinalities for directory measurements; never provisions real recipients.</summary>
public static class DirectoryFixtureData
{
    /// <summary>Adds confirmed profiles with deterministic names/numbers and a Manager beyond the first page.</summary>
    public static async Task SeedAsync(ApiFixture fixture, int count)
    {
        await using var db = fixture.Context();
        var department = fixture.Employee("employee").DepartmentId;
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name!, r => r.Id);
        for (var i = 0; i < count; i++)
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = $"directory-{i:D4}@synthetic.invalid",
                UserName = $"directory-{i:D4}@synthetic.invalid", NormalizedEmail = $"DIRECTORY-{i:D4}@SYNTHETIC.INVALID",
                NormalizedUserName = $"DIRECTORY-{i:D4}@SYNTHETIC.INVALID", RequiresActivation = false };
            db.Users.Add(user);
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roles[i == count - 1 ? "Manager" : "Employee"] });
            var employee = Employee.Create($"Directory person {i:D4}", user.Email, department);
            employee.ConfirmEmployment($"DIR-{i:D4}", new DateOnly(2020, 1, 1));
            employee.SetIdentityUser(user.Id.ToString()); db.Employees.Add(employee);
        }
        await db.SaveChangesAsync();
    }
}
