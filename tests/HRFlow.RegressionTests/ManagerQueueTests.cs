using System.Data.Common;
using System.Net;
using System.Text.Json.Nodes;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Real HTTP, persisted outcomes and deferred SQLite reads prove bounded queue scope without replacing decision gates.</summary>
public sealed class ManagerQueueTests
{
    /// <summary>Filters precede paging, ties are stable, and unknown/out-of-scope IDs reveal no rows.</summary>
    [Fact]
    public async Task FiltersOrderBoundsAndReadOnlyScope()
    {
        await using var f = await ApiFixture.StartAsync();
        var ids = new List<Guid>();
        await using (var db = f.Context())
        {
            var type = await db.LeaveTypes.Select(t => t.Id).SingleAsync();
            foreach (var i in Enumerable.Range(0, 55))
            {
                var r = LeaveRequest.Create(f.Employee("employee").Id, type,
                    new DateTime(2028, 2, 28).AddDays(i / 20), new DateTime(2028, 3, 1));
                ids.Add(r.Id); db.LeaveRequests.Add(r);
            }
            foreach (var owner in new[] { "manager", "cross", "unrelated" })
                db.LeaveRequests.Add(LeaveRequest.Create(f.Employee(owner).Id, type, new(2028, 2, 28), new(2028, 3, 1)));
            await db.SaveChangesAsync();
        }
        await using var original = f.Context();
        var originalAudits = await original.AuditEntries.AsNoTracking().OrderBy(a => a.Id).Select(a => new { a.Id, a.Action, a.Timestamp, a.NewStatus }).ToArrayAsync();
        var token = await f.LoginAsync("manager");
        var first = await Read(f, token, ""); var second = await Read(f, token, "page=2"); var third = await Read(f, token, "page=3");
        using var attention = await f.SendAsync(token, "leave-requests/manager-attention"); Assert.Equal(55, (await ApiFixture.JsonAsync(attention))["pendingCount"]!.GetValue<int>());
        Assert.Equal(55, first["totalCount"]!.GetValue<int>()); Assert.Equal(25, first["items"]!.AsArray().Count);
        var actual = new[] { first, second, third }.SelectMany(p => p["items"]!.AsArray()).Select(n => n!["id"]!.GetValue<Guid>()).ToArray();
        await using var check = f.Context();
        var expected = await check.LeaveRequests.Where(r => r.EmployeeId == f.Employee("employee").Id).OrderBy(r => r.StartDate).ThenBy(r => r.Id).Select(r => r.Id).ToArrayAsync();
        Assert.Equal(expected, actual); Assert.Equal(55, actual.Distinct().Count());
        Assert.Equal(3, (await Read(f, token, "page=2147483647"))["page"]!.GetValue<int>());
        Assert.Equal(55, (await Read(f, token, "pageSize=100"))["items"]!.AsArray().Count);
        var typeId = await check.LeaveTypes.Select(t => t.Id).SingleAsync();
        Assert.Equal(55, (await Read(f, token, $"employeeId={f.Employee("employee").Id}&leaveTypeId={typeId}&start=2028-02-29&end=2028-03-01"))["totalCount"]!.GetValue<int>());
        Assert.Equal(0, (await Read(f, token, $"employeeId={f.Employee("cross").Id}"))["totalCount"]!.GetValue<int>());
        Assert.Equal(0, (await Read(f, token, $"leaveTypeId={Guid.NewGuid()}"))["totalCount"]!.GetValue<int>());
        Assert.Equal(0, (await Read(f, token, "start=2028-03-02"))["totalCount"]!.GetValue<int>());
        foreach (var query in new[] { "page=0", "page=-1", "page=2147483648", "pageSize=101", "pageSize=0", "start=2028-02-30", "start=2028-03-01&end=2028-02-29", "employeeId=bad", "employeeId=" + Guid.Empty, "status=Approved" })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.SendAsync(token, "leave-requests?" + query)).StatusCode);
        using var people = await f.SendAsync(token, "leave-requests/queue-people");
        var choices = (await ApiFixture.JsonAsync(people)).AsArray(); Assert.Single(choices); Assert.Equal(f.Employee("employee").Id, choices[0]!["id"]!.GetValue<Guid>());
        Assert.Equal(HttpStatusCode.BadRequest, (await f.SendAsync(token, "leave-requests/queue-people?search=" + new string('x', 101))).StatusCode);
        Assert.Equal(58, await check.LeaveRequests.CountAsync()); Assert.Equal(originalAudits, await check.AuditEntries.AsNoTracking().OrderBy(a => a.Id).Select(a => new { a.Id, a.Action, a.Timestamp, a.NewStatus }).ToArrayAsync());
        Assert.Equal(0, await check.LeaveSubmissionOutcomes.CountAsync()); Assert.False(check.ChangeTracker.HasChanges());
    }

    /// <summary>Current roles/lifecycle/credential proof, not token claims or supplied scope IDs, govern every read.</summary>
    [Fact]
    public async Task LiveAuthorizationAndReassignment()
    {
        await using var f = await ApiFixture.StartAsync(); var id = await f.PendingAsync("employee");
        foreach (var actor in new[] { "employee", "hr" })
        {
            var denied = await f.LoginAsync(actor);
            Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(denied, "leave-requests")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(denied, "leave-requests/queue-people")).StatusCode);
        }
        var combined = await f.LoginAsync("combined"); Assert.Equal(0, (await Read(f, combined, ""))["totalCount"]!.GetValue<int>());
        var manager = await f.LoginAsync("manager"); Assert.Equal(1, (await Read(f, manager, "requestId=" + id))["totalCount"]!.GetValue<int>());
        await using var db = f.Context();
        await db.Employees.Where(e => e.Id == f.Employee("employee").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.ManagerId, (Guid?)f.Employee("combined").Id));
        Assert.Equal(0, (await Read(f, manager, "requestId=" + id))["totalCount"]!.GetValue<int>());
        Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(manager, $"leave-requests/{id}/approve", HttpMethod.Post, new { })).StatusCode);
        var role = await db.Roles.SingleAsync(r => r.Name == "Manager");
        await db.UserRoles.Where(r => r.UserId == f.User("manager").Id && r.RoleId == role.Id).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(manager, "leave-requests")).StatusCode);
        var rotated = Guid.NewGuid(); await db.Users.Where(u => u.Id == f.User("combined").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CredentialVersion, rotated));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(combined, "leave-requests")).StatusCode);
        Assert.Equal(1, await db.AuditEntries.CountAsync(a => a.Action == "Submit"));
    }

    /// <summary>Decisions from separate API processes remain serialized; the emptied final page is safely clamped.</summary>
    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task DecisionRefreshAndConcurrentDecision(string action)
    {
        await using var f = await ApiFixture.StartAsync(); var id = await f.SubmitAsync("employee"); var manager = await f.LoginAsync("manager");
        var results = await Task.WhenAll(f.SendAsync(manager, $"leave-requests/{id}/{action}", HttpMethod.Post, new { decisionNote = "Synthetic retained note" }),
            f.SendAsync(manager, $"leave-requests/{id}/{action}", HttpMethod.Post, new { decisionNote = "Synthetic retained note" }, server: 1));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        var page = await Read(f, manager, "page=2147483647"); Assert.Equal(0, page["totalCount"]!.GetValue<int>()); Assert.Equal(1, page["page"]!.GetValue<int>());
        await using var db = f.Context(); Assert.Equal(2, await db.AuditEntries.CountAsync());
        Assert.Single(await db.AuditEntries.Where(a => a.Action == (action == "approve" ? "Approve" : "Reject") && a.DecisionNote == "Synthetic retained note").ToArrayAsync());
    }

    /// <summary>The handler uses the same deferred transaction for credential/role proof, count and page; command count is constant.</summary>
    [Theory]
    [InlineData(20)]
    [InlineData(2000)]
    public async Task ConstantProtectedCommands(int size)
    {
        await using var f = await ApiFixture.StartAsync();
        await using (var seed = f.Context())
        {
            var type = await seed.LeaveTypes.Select(t => t.Id).SingleAsync();
            seed.LeaveRequests.AddRange(Enumerable.Range(0, size).Select(_ => LeaveRequest.Create(f.Employee("employee").Id, type, new(2028, 1, 1), new(2028, 1, 1)))); await seed.SaveChangesAsync();
        }
        var counter = new Counter(); await using var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=" + f.DatabasePath).AddInterceptors(counter).Options);
        var access = new AccountAccessService(db); var user = f.User("manager");
        var reads = new SqliteTeamLeaveReadTransaction(db, new RequestCredentialValidator(new Credential(user.Id.ToString(), user.CredentialVersion.ToString()), access));
        var handler = new GetManagerQueueQueryHandler(db, reads, new CurrentAccountAuthorization(db, new EmployeeRoleLookupService(db), access), TimeProvider.System);
        var result = await handler.Handle(new(user.Id, new()), default);
        Assert.Equal(size, result.TotalCount); Assert.Equal(Math.Min(25, size), result.Items.Count); Assert.Equal(6, counter.Count); Assert.True(counter.OneTransaction);
        Console.WriteLine($"queue-selects rows={size} commands={counter.Count} protected={counter.OneTransaction}");
    }

    /// <summary>Selector cardinality is bounded independently of queue pages, with searches reaching later eligible owners.</summary>
    [Fact]
    public async Task PeopleSearchBeyondFirstPageAndDeactivation()
    {
        await using var f = await ApiFixture.StartAsync(); await DirectoryFixtureData.SeedAsync(f, 30);
        await using var db = f.Context(); var type = await db.LeaveTypes.Select(t => t.Id).SingleAsync();
        var people = await db.Employees.Where(e => e.Email.StartsWith("directory-")).ToArrayAsync();
        foreach (var person in people) { person.AssignManager(f.Employee("manager").Id); db.LeaveRequests.Add(LeaveRequest.Create(person.Id, type, new(2028, 1, 1), new(2028, 1, 1))); }
        await db.SaveChangesAsync(); var manager = await f.LoginAsync("manager");
        using var all = await f.SendAsync(manager, "leave-requests/queue-people"); Assert.Equal(25, (await ApiFixture.JsonAsync(all)).AsArray().Count);
        using var last = await f.SendAsync(manager, "leave-requests/queue-people?search=0029");
        var selected = (await ApiFixture.JsonAsync(last)).AsArray(); Assert.Single(selected);
        var owner = selected[0]!["id"]!.GetValue<Guid>(); Assert.Equal(1, (await Read(f, manager, "employeeId=" + owner))["totalCount"]!.GetValue<int>());
        var hr = await f.LoginAsync("hr"); using var detail = await f.SendAsync(hr, "employees/" + owner);
        var version = (await ApiFixture.JsonAsync(detail))["version"]!.GetValue<Guid>();
        using var deactivated = await f.SendAsync(hr, $"employees/{owner}/deactivate", HttpMethod.Patch, new { expectedVersion = version, reason = "Synthetic lifecycle" });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.Equal(0, (await Read(f, manager, "employeeId=" + owner))["totalCount"]!.GetValue<int>());
        Assert.Equal(31, await db.AuditEntries.CountAsync());
    }

    /// <summary>Already-issued tokens never retain inactive or pending Manager access to either bounded read.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LifecycleDenial(bool pending)
    {
        await using var f = await ApiFixture.StartAsync(); var manager = await f.LoginAsync("manager");
        await using var db = f.Context();
        if (pending) await db.Users.Where(u => u.Id == f.User("manager").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.RequiresActivation, true));
        else (await db.Employees.FindAsync(f.Employee("manager").Id))!.Deactivate(f.Employee("hr").Id, "Synthetic lifecycle");
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(manager, "leave-requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(manager, "leave-requests/queue-people")).StatusCode);
    }

    private sealed record Credential(string? IdentityUserId, string? Version) : IInitiatingCredential;
    private sealed class Counter : DbCommandInterceptor
    {
        public int Count; public bool OneTransaction = true; private DbTransaction? transaction;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken token = default)
        { Count++; transaction ??= command.Transaction; OneTransaction &= command.Transaction != null && ReferenceEquals(transaction, command.Transaction); return ValueTask.FromResult(result); }
    }
    private static async Task<JsonNode> Read(ApiFixture f, string token, string query)
    {
        using var response = await f.SendAsync(token, "leave-requests?" + query); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await ApiFixture.JsonAsync(response);
    }
}
