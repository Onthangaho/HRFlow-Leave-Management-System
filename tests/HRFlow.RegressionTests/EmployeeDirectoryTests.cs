using System.Data.Common;
using System.Net;
using HRFlow.Application.Features.Employees.Queries.GetEmployees;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services.Employees;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Real SQLite directory reads verify bounded projections and current server authorization.</summary>
public sealed class EmployeeDirectoryTests
{
    /// <summary>HR presence hints omit retained absence generations without changing own-account concurrency history.</summary>
    [Fact]
    public async Task ImageHintsDistinguishContentFromRetainedAbsenceVersions()
    {
        await using var fixture = await ApiFixture.StartAsync();
        var hr = await fixture.LoginAsync("hr"); var owner = await fixture.LoginAsync("employee");
        var employee = fixture.Employee("employee").Id;

        async Task AssertHints(Guid? expected)
        {
            await using var before = fixture.Context();
            var original = await before.ProfileImages.AsNoTracking().OrderBy(i => i.Id).ToArrayAsync();
            var requests = await before.LeaveRequests.CountAsync(); var audits = await before.AuditEntries.CountAsync();
            using var page = await fixture.SendAsync(hr, "employees?search=" + Uri.EscapeDataString(fixture.Employee("employee").Email));
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var item = Assert.Single((await ApiFixture.JsonAsync(page))["items"]!.AsArray());
            using var detail = await fixture.SendAsync(hr, $"employees/{employee}");
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            Assert.Equal(expected, item!["profileImageVersion"]?.GetValue<Guid>());
            Assert.Equal(expected, (await ApiFixture.JsonAsync(detail))["profileImageVersion"]?.GetValue<Guid>());
            await using var after = fixture.Context();
            var remaining = await after.ProfileImages.AsNoTracking().OrderBy(i => i.Id).ToArrayAsync();
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(original), System.Text.Json.JsonSerializer.Serialize(remaining));
            Assert.Equal(requests, await after.LeaveRequests.CountAsync()); Assert.Equal(audits, await after.AuditEntries.CountAsync());
        }

        await AssertHints(null);
        // Projection fixtures use real processed raster facts, not a scanner substitute or upload-security claim.
        using var input = new MemoryStream(ProfileImageTests.Raster());
        var storage = new HRFlow.Infrastructure.Services.PrivateProfileImageStorage(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var bytes = await storage.ProcessAsync(input, ".png", "image/png", new(0, 0, 1), default);
        var image = new ProfileImage { OwnerId = employee, Status = ProfileImageStatus.Current, Size = bytes.Length,
            Checksum = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) };
        await using (var db = fixture.Context()) { db.ProfileImages.Add(image); await db.SaveChangesAsync(); }
        await AssertHints(image.Version);
        using var removed = await fixture.SendAsync(owner, $"me/image?expectedVersion={image.Version}", HttpMethod.Delete);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Guid absenceVersion;
        await using (var db = fixture.Context())
        {
            Assert.Equal(ProfileImageStatus.Removed, (await db.ProfileImages.FindAsync(image.Id))!.Status);
            var absence = await db.ProfileImages.SingleAsync(i => i.OwnerId == employee && i.Status == ProfileImageStatus.Current);
            Assert.Equal(0, absence.Size); Assert.NotEqual(image.Version, absence.Version); absenceVersion = absence.Version;
        }
        await AssertHints(null);
        using var own = await fixture.SendAsync(owner, "me/image");
        var metadata = await ApiFixture.JsonAsync(own);
        Assert.Equal(absenceVersion, metadata["version"]!.GetValue<Guid>()); Assert.False(metadata["hasImage"]!.GetValue<bool>());
        using var stale = await fixture.SendAsync(owner, $"me/image?expectedVersion={image.Version}", HttpMethod.Delete);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var replacement = new ProfileImage { OwnerId = employee, Status = ProfileImageStatus.Current, Size = bytes.Length, Checksum = image.Checksum };
        await using (var db = fixture.Context())
        {
            var absence = await db.ProfileImages.SingleAsync(i => i.OwnerId == employee && i.Status == ProfileImageStatus.Current);
            absence.Status = ProfileImageStatus.Removed; await db.SaveChangesAsync();
            db.ProfileImages.Add(replacement); await db.SaveChangesAsync();
        }
        Assert.NotEqual(absenceVersion, replacement.Version); await AssertHints(replacement.Version);
        await using var final = fixture.Context();
        Assert.Equal(3, await final.ProfileImages.CountAsync(i => i.OwnerId == employee));
        Assert.Equal(ProfileImageStatus.Removed, (await final.ProfileImages.SingleAsync(i => i.Version == absenceVersion)).Status);
    }

    /// <summary>Data command count stays constant as directory cardinality grows; gate queries are measured separately from this reader.</summary>
    [Fact]
    public async Task ProjectionUsesThreeCommandsAndStablePages()
    {
        await using var fixture = await ApiFixture.StartAsync();
        await DirectoryFixtureData.SeedAsync(fixture, 1000);
        var counter = new CommandCounter();
        await using var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>()
            .UseSqlite("Data Source=" + fixture.DatabasePath + ";Foreign Keys=True").AddInterceptors(counter).Options);
        var reader = new EmployeeDirectoryReader(db, TimeProvider.System);
        var first = await reader.ReadAsync(new(), default);
        Assert.Equal(3, counter.Count); Assert.Equal(1007, first.TotalCount); Assert.Equal(25, first.Items.Count);
        var second = await reader.ReadAsync(new() { Page = 2 }, default);
        Assert.Empty(first.Items.Select(e => e.Id).Intersect(second.Items.Select(e => e.Id)));
        Assert.All(first.Items, e => Assert.Equal(new[] { "Employee" }, e.Roles));
        var last = await reader.ReadAsync(new() { Page = 41 }, default); Assert.Equal(7, last.Items.Count);
        var empty = await reader.ReadAsync(new() { Page = 42 }, default); Assert.Empty(empty.Items); Assert.Equal(1007, empty.TotalCount);
        var search = await reader.ReadAsync(new() { Search = "dir-0999", DepartmentId = fixture.Employee("employee").DepartmentId, Status = "Active" }, default);
        Assert.Single(search.Items); Assert.Equal("Directory person 0999", search.Items[0].FullName);
        var managers = await reader.ManagersAsync(fixture.Employee("employee").DepartmentId, fixture.Employee("employee").Id, "0999", 1, default);
        Assert.Single(managers.Items); Assert.Equal(fixture.Employee("manager").Id, managers.AssignedManager!.Id);
        await using var original = fixture.Context();
        var duplicates = await original.Employees.Where(e => e.EmployeeNumber == "DIR-0000" || e.EmployeeNumber == "DIR-0001").Select(e => e.Id).ToArrayAsync();
        await original.Employees.Where(e => duplicates.Contains(e.Id)).ExecuteUpdateAsync(set => set.SetProperty(e => e.FullName, "Duplicate name"));
        var duplicatePage = await reader.ReadAsync(new() { Search = "Duplicate name", PageSize = 1 }, default);
        var duplicateNext = await reader.ReadAsync(new() { Search = "Duplicate name", PageSize = 1, Page = 2 }, default);
        Assert.Equal(2, duplicatePage.TotalCount); Assert.NotEqual(duplicatePage.Items[0].Id, duplicateNext.Items[0].Id);
        Assert.Equal(duplicatePage.Items[0].Id, (await reader.ReadAsync(new() { Search = "Duplicate name", PageSize = 1 }, default)).Items[0].Id);
        Assert.Empty(await original.LeaveRequests.ToArrayAsync()); Assert.Empty(await original.AuditEntries.ToArrayAsync());
        Assert.Equal(1007, await original.Users.CountAsync()); Assert.Equal(1009, await original.UserRoles.CountAsync());
        var managerRole = await original.Roles.Where(r => r.Name == "Manager").Select(r => r.Id).SingleAsync();
        var extraManagers = await original.Employees.Where(e => e.EmployeeNumber != null && e.EmployeeNumber.StartsWith("DIR-00"))
            .OrderBy(e => e.EmployeeNumber).Take(40).Select(e => e.IdentityUserId).ToArrayAsync();
        original.UserRoles.AddRange(extraManagers.Select(id => new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { UserId = Guid.Parse(id!), RoleId = managerRole }));
        await original.SaveChangesAsync();
        var managerFirst = await reader.ManagersAsync(fixture.Employee("employee").DepartmentId, null, null, 1, default);
        var managerNext = await reader.ManagersAsync(fixture.Employee("employee").DepartmentId, null, null, 2, default);
        Assert.Equal(25, managerFirst.Items.Count); Assert.NotEmpty(managerNext.Items);
        Assert.Empty(managerFirst.Items.Select(m => m.Id).Intersect(managerNext.Items.Select(m => m.Id)));
    }

    /// <summary>Attention filters match current SAST definitions across the whole store, including expired/unlinked states.</summary>
    [Fact]
    public async Task AttentionFiltersAndDetailRemainConsistent()
    {
        await using var fixture = await ApiFixture.StartAsync(); await DirectoryFixtureData.SeedAsync(fixture, 60);
        await using var db = fixture.Context();
        var target = await db.Employees.SingleAsync(e => e.EmployeeNumber == "DIR-0050");
        var user = await db.Users.SingleAsync(u => u.Id == Guid.Parse(target.IdentityUserId!));
        user.RequiresActivation = true; user.InvitationDeliveryState = ActivationDeliveryStates.DeliveryFailed;
        user.ActivationExpiresAtUtc = DateTime.UtcNow.AddDays(1);
        db.WeeklyScheduleRevisions.Add(WeeklyScheduleRevision.Create(target.Id, fixture.Employee("hr").Id, "Future", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), [480, 0, 0, 0, 0, 0, 0]));
        await db.SaveChangesAsync();
        var reader = new EmployeeDirectoryReader(db, TimeProvider.System);
        Assert.Equal(60, (await reader.ReadAsync(new() { Attention = "schedule" }, default)).TotalCount);
        Assert.Equal(target.Id, Assert.Single((await reader.ReadAsync(new() { Attention = "future-schedule" }, default)).Items).Id);
        Assert.Equal(target.Id, Assert.Single((await reader.ReadAsync(new() { Attention = "delivery-failed" }, default)).Items).Id);
        Assert.Single((await reader.ReadAsync(new() { Attention = "activation" }, default)).Items);
        user.InvitationDeliveryState = ActivationDeliveryStates.PendingDelivery; await db.SaveChangesAsync();
        Assert.Single((await reader.ReadAsync(new() { Attention = "delivery-pending" }, default)).Items);
        user.ActivationExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        Assert.Empty((await reader.ReadAsync(new() { Attention = "delivery-pending" }, default)).Items);
        Assert.Equal("Expired", (await reader.DetailAsync(target.Id, default))!.InvitationDeliveryState);
        Assert.Equal(7, (await reader.ReadAsync(new() { Attention = "employment" }, default)).TotalCount);
        await db.Employees.Where(e => e.Id == target.Id).ExecuteUpdateAsync(set => set.SetProperty(e => e.IsActive, false));
        Assert.Equal(1, (await reader.ReadAsync(new() { Status = "Inactive", Search = "DIR-0050" }, default)).TotalCount);
        Assert.Empty((await reader.ManagersAsync(target.DepartmentId, null, "0059", 2, default)).Items);
    }

    /// <summary>API gates use current roles/lifecycle, not an old JWT; malformed filters fail without writes.</summary>
    [Fact]
    public async Task HttpAuthorizationAndBounds()
    {
        await using var fixture = await ApiFixture.StartAsync();
        var hr = await fixture.LoginAsync("hr"); var combined = await fixture.LoginAsync("combined");
        foreach (var actor in new[] { "employee", "manager" })
            Assert.Equal(HttpStatusCode.Forbidden, (await fixture.SendAsync(await fixture.LoginAsync(actor), "employees")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(hr, "employees")).StatusCode);
        foreach (var invalid in new[] { "page=0", "pageSize=101", "pageSize=0", "attention=unknown", "sort=unknown", "status=unknown", "departmentId=" + Guid.NewGuid(), "search=" + new string('x', 101) })
            Assert.Equal(HttpStatusCode.BadRequest, (await fixture.SendAsync(hr, "employees?" + invalid)).StatusCode);
        await using var db = fixture.Context(); var hrRole = await db.Roles.SingleAsync(r => r.Name == "HR Administrator");
        var combinedId = fixture.User("combined").Id;
        await db.UserRoles.Where(r => r.UserId == combinedId && r.RoleId == hrRole.Id).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.SendAsync(combined, "employees")).StatusCode);
        await db.Users.Where(u => u.Id == fixture.User("hr").Id).ExecuteUpdateAsync(set => set.SetProperty(u => u.RequiresActivation, true));
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.SendAsync(hr, "employees")).StatusCode);
        await db.Users.Where(u => u.Id == fixture.User("hr").Id).ExecuteUpdateAsync(set => set.SetProperty(u => u.RequiresActivation, false));
        await db.Employees.Where(e => e.Id == fixture.Employee("hr").Id).ExecuteUpdateAsync(set => set.SetProperty(e => e.IsActive, false));
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.SendAsync(hr, "employees")).StatusCode);
        Assert.Empty(await db.LeaveRequests.ToArrayAsync()); Assert.Empty(await db.AuditEntries.ToArrayAsync());
    }
    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
    }
}
