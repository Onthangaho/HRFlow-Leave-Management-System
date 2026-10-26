using System.Security.Cryptography;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Reproduces an earlier real schema and synthetic history without committing a database or guessed legacy events.</summary>
public sealed class MigrationTests
{
    /// <summary>Upgrades original Identity/profile/request/audit rows and leaves newly introduced employment/snapshot facts Unknown.</summary>
    [Fact]
    public async Task LegacyRecordsSurviveUpgrade()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrflow-legacy-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); ApiFixture.RestrictDirectory(root);
        try
        {
            await using var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=" + Path.Combine(root, "legacy.sqlite") + ";Foreign Keys=True").Options);
            await db.GetService<IMigrator>().MigrateAsync("20261002094357_EnforceUniqueLeaveRequestAuditTransitions");
            var user = Guid.NewGuid(); var employee = Guid.NewGuid(); var dept = Guid.NewGuid(); var policy = Guid.NewGuid(); var type = Guid.NewGuid(); var request = Guid.NewGuid(); var audit = Guid.NewGuid(); var role = Guid.NewGuid();
            var hash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), "Aa!9" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)));
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AspNetUsers(Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount) VALUES({user},{"legacy@synthetic.invalid"},{"LEGACY@SYNTHETIC.INVALID"},{"legacy@synthetic.invalid"},{"LEGACY@SYNTHETIC.INVALID"},1,{hash},{Guid.NewGuid().ToString()},0,0,1,0)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AspNetRoles(Id,Name,NormalizedName) VALUES({role},{"Employee"},{"EMPLOYEE"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AspNetUserRoles(UserId,RoleId) VALUES({user},{role})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Departments(Id,Name) VALUES({dept},{"Synthetic legacy department"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Employees(Id,FullName,Email,DepartmentId,IdentityUserId) VALUES({employee},{"Synthetic legacy employee"},{"legacy@synthetic.invalid"},{dept},{user.ToString()})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO LeavePolicies(Id,AllowOverlap,DefaultBalance) VALUES({policy},0,20)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO LeaveTypes(Id,Name,LeavePolicyId) VALUES({type},{"Synthetic legacy leave"},{policy})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO LeaveRequests(Id,EmployeeId,LeaveTypeId,StartDate,EndDate,Status,ProcessedById,ProcessedOn) VALUES({request},{employee},{type},{"2026-01-01 00:00:00"},{"2026-01-02 00:00:00"},1,{employee},{"2026-01-03 12:00:00"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AuditEntries(Id,LeaveRequestId,ActorId,Action,OldStatus,NewStatus,Timestamp) VALUES({audit},{request},{employee},{"Approve"},0,1,{"2026-01-03 12:00:00"})");
            await db.Database.MigrateAsync();
            var profile = await db.Employees.SingleAsync(); Assert.Equal(employee, profile.Id); Assert.Equal("Synthetic legacy employee", profile.FullName);
            Assert.Null(profile.EmployeeNumber); Assert.Null(profile.EmploymentStartDate); Assert.Empty(await db.WeeklyScheduleRevisions.ToListAsync()); Assert.True(profile.IsActive);
            Assert.Equal(dept, profile.DepartmentId); Assert.Equal(user.ToString(), profile.IdentityUserId);
            var identity = await db.Users.SingleAsync(); Assert.Equal(user, identity.Id); Assert.True(hash == identity.PasswordHash, "Legacy password hash must remain unchanged."); Assert.False(identity.RequiresActivation); Assert.Null(identity.ActivationRecipientNormalizedEmail); Assert.Null(identity.ActivationDispatchStartedAtUtc); Assert.Equal("LEGACY@SYNTHETIC.INVALID", identity.NormalizedEmail);
            var link = Assert.Single(await db.UserRoles.ToListAsync()); Assert.Equal(user, link.UserId); Assert.Equal(role, link.RoleId);
            var leave = await db.LeaveRequests.SingleAsync(); Assert.Equal(request, leave.Id); Assert.Equal(LeaveRequestStatus.Approved, leave.Status); Assert.Equal(employee, leave.ProcessedById);
            Assert.Equal(new DateTime(2026, 1, 1), leave.StartDate); Assert.Equal(new DateTime(2026, 1, 2), leave.EndDate); Assert.Null(leave.SubmissionRequirements); Assert.Null(leave.Description);
            var entry = await db.AuditEntries.SingleAsync(); Assert.Equal(audit, entry.Id); Assert.Equal(employee, entry.ActorId); Assert.Equal("Approve", entry.Action);
            Assert.Equal(new DateTime(2026, 1, 3, 12, 0, 0), entry.Timestamp); Assert.Null(entry.CorrelationId); Assert.Null(entry.DecisionNote);
            Assert.Empty(await db.AuditEntries.Where(a => a.OldStatus == null).ToListAsync());
            Assert.Empty(await db.EvidenceAdditions.ToListAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.Ordinal) && Path.GetFileName(root).StartsWith("hrflow-legacy-")) Directory.Delete(root, recursive: true);
        }
    }
}
