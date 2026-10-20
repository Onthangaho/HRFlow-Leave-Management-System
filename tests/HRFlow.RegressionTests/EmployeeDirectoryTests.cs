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
