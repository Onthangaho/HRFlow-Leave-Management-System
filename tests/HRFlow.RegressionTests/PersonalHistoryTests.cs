using System.Data.Common;
using System.Net;
using System.Text.Json.Nodes;
using HRFlow.Application.Features.LeaveRequests.Queries.GetEmployeeLeaveHistory;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Real HTTP and independently inspected SQLite rows protect personal scope and bounded immutable history.</summary>
public sealed class PersonalHistoryTests
{
    /// <summary>Filters precede paging, ties are deterministic, unavailable types do not disclose other owners, reads do not write.</summary>
    [Fact]
    public async Task FiltersOrderBoundsAndPreservedAudits()
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("employee");
        var start = new DateTime(2028, 2, 28);
        await using var db = f.Context(); var type = await db.LeaveTypes.SingleAsync();
        var rows = Enumerable.Range(0, 55).Select(i => LeaveRequest.Create(f.Employee("employee").Id, type.Id,
            start.AddDays(i / 3), start.AddDays(i / 3 + 2))).ToArray();
        rows[0].Approve(f.Employee("manager").Id, decisionNote: "Synthetic plain <text>");
        rows[1].Reject(f.Employee("manager").Id); rows[2].Cancel(f.Employee("employee").Id);
        db.LeaveRequests.AddRange(rows);
        db.LeaveRequests.Add(LeaveRequest.Create(f.Employee("unrelated").Id, type.Id, start, start));
        await db.SaveChangesAsync();
        var audits = await db.AuditEntries.AsNoTracking().OrderBy(a => a.Id).ToArrayAsync();
        var expected = await db.LeaveRequests.Where(r => r.EmployeeId == f.Employee("employee").Id)
            .OrderByDescending(r => r.StartDate).ThenByDescending(r => r.Id).Select(r => r.Id).ToArrayAsync();
        var all = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var data = await Read(f, token, "page=" + page);
            Assert.Equal(55, data["totalCount"]!.GetValue<int>()); Assert.Equal(page, data["page"]!.GetValue<int>());
            Assert.Equal(25, data["pageSize"]!.GetValue<int>());
            all.AddRange(data["items"]!.AsArray().Select(r => r!["id"]!.GetValue<Guid>()));
            Assert.Equal("no-store", (await f.SendAsync(token, "leave-requests/history")).Headers.CacheControl!.ToString());
        }
        Assert.Equal(expected, all);
        Assert.Equal(55, (await Read(f, token, "pageSize=100"))["items"]!.AsArray().Count);
        foreach (var status in new[] { "Pending", "Approved", "Rejected", "Cancelled" })
            Assert.Equal(status == "Pending" ? 52 : 1, (await Read(f, token, "status=" + status))["totalCount"]!.GetValue<int>());
        var extreme = await Read(f, token, "page=2147483647"); Assert.Equal(3, extreme["page"]!.GetValue<int>());
        var boundary = await Read(f, token, "status=Approved&start=2028-03-01&end=2028-03-01&leaveTypeId=" + type.Id);
        Assert.Equal(1, boundary["totalCount"]!.GetValue<int>());
        var decision = boundary["items"]![0]!["decisionHistory"]!.AsArray().Last()!;
        Assert.Equal("Synthetic plain <text>", decision["decisionNote"]!.GetValue<string>());
        Assert.EndsWith("Z", decision["timestamp"]!.GetValue<string>());
        Assert.Equal(0, (await Read(f, token, "start=2028-03-02&status=Approved"))["totalCount"]!.GetValue<int>());
        Assert.Equal(1, (await Read(f, token, "end=2028-02-28&status=Approved"))["totalCount"]!.GetValue<int>());
        Assert.Equal(0, (await Read(f, token, "leaveTypeId=" + Guid.NewGuid()))["totalCount"]!.GetValue<int>());
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(audits), System.Text.Json.JsonSerializer.Serialize(
            await db.AuditEntries.AsNoTracking().OrderBy(a => a.Id).ToArrayAsync()));
        Assert.Equal(56, await db.LeaveRequests.CountAsync());
        Assert.Empty(await db.LeaveSubmissionOutcomes.ToArrayAsync()); Assert.Empty(await db.LeaveNotificationEvents.ToArrayAsync());
        foreach (var bad in new[] { "page=0", "page=-1", "page=99999999999999", "pageSize=0", "pageSize=101", "status=1", "status=unknown", "start=bad", "start=2028-03-01&end=2028-02-01", "leaveTypeId=bad", "leaveTypeId=" + Guid.Empty })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.SendAsync(token, "leave-requests/history?" + bad)).StatusCode);
    }

    /// <summary>Owner is authentication-derived even with overposted query scope; live role/lifecycle/credentials remain authoritative.</summary>
    [Fact]
    public async Task CurrentScopeAndEmptyHistories()
    {
        await using var f = await ApiFixture.StartAsync();
        var id = await f.PendingAsync("employee");
        foreach (var actor in new[] { "employee", "manager", "combined", "unrelated" })
        {
            var result = await Read(f, await f.LoginAsync(actor), "employeeId=" + f.Employee("employee").Id);
            Assert.Equal(actor == "employee" ? 1 : 0, result["totalCount"]!.GetValue<int>());
            Assert.Equal(1, result["page"]!.GetValue<int>());
        }
        var hr = await f.LoginAsync("hr");
        Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(hr, "leave-requests/history")).StatusCode);
        var owner = await f.LoginAsync("employee");
        await using var db = f.Context(); var user = f.User("employee").Id;
        await db.UserRoles.Where(r => r.UserId == user).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(owner, "leave-requests/history")).StatusCode);
        var manager = await f.LoginAsync("manager");
        await db.Employees.Where(e => e.Id == f.Employee("manager").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(manager, "leave-requests/history")).StatusCode);
        var combined = await f.LoginAsync("combined");
        await db.Users.Where(u => u.Id == f.User("combined").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.RequiresActivation, true));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(combined, "leave-requests/history")).StatusCode);
        var unrelated = await f.LoginAsync("unrelated");
        var revokedVersion = Guid.NewGuid();
        await db.Users.Where(u => u.Id == f.User("unrelated").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CredentialVersion, revokedVersion));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(unrelated, "leave-requests/history")).StatusCode);
        Assert.Single(await db.LeaveRequests.ToArrayAsync()); Assert.Single(await db.AuditEntries.ToArrayAsync());
    }

    /// <summary>Successful and explicitly recovered submissions appear once; the later cancellation remains visible through filters.</summary>
    [Fact]
    public async Task HistoryAfterSubmissionAndDurableRecovery()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee");
        await using var db = f.Context(); var type = await db.LeaveTypes.Include(t => t.LeavePolicy).SingleAsync();
        var body = new { submissionKey = Guid.NewGuid(), leaveTypeId = type.Id, startDate = DateTime.UtcNow.Date.AddDays(15),
            endDate = DateTime.UtcNow.Date.AddDays(16), expectedTypeVersion = type.Version, expectedPolicyVersion = type.LeavePolicy.Version };
        using var submitted = await f.SendAsync(owner, "leave-requests", HttpMethod.Post, body);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode); var id = (await ApiFixture.JsonAsync(submitted)).GetValue<Guid>();
        using var recovered = await f.SendAsync(owner, "leave-requests/submission-outcome", HttpMethod.Post, body);
        Assert.Equal(id, (await ApiFixture.JsonAsync(recovered))["requestId"]!.GetValue<Guid>());
        Assert.Equal(id, (await Read(f, owner, "status=Pending"))["items"]![0]!["id"]!.GetValue<Guid>());
        Assert.Equal(HttpStatusCode.NoContent, (await f.SendAsync(owner, $"leave-requests/{id}/cancel", HttpMethod.Post)).StatusCode);
        var history = await Read(f, owner, "status=Cancelled"); Assert.Equal(1, history["totalCount"]!.GetValue<int>());
        Assert.Equal(2, history["items"]![0]!["decisionHistory"]!.AsArray().Count);
        Assert.Single(await db.LeaveRequests.ToArrayAsync()); Assert.Single(await db.LeaveSubmissionOutcomes.ToArrayAsync());
        Assert.Equal(2, await db.AuditEntries.CountAsync());
    }

    /// <summary>A real cancellation shrinks a filtered final page; the server returns its new last page without rewriting audits.</summary>
    [Fact]
    public async Task CancellationClampsFilteredFinalPage()
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("employee");
        var first = await f.PendingAsync("employee"); var second = await f.PendingAsync("employee");
        var last = await Read(f, token, "status=Pending&pageSize=1&page=2");
        var id = last["items"]![0]!["id"]!.GetValue<Guid>();
        Assert.Equal(HttpStatusCode.NoContent, (await f.SendAsync(token, $"leave-requests/{id}/cancel", HttpMethod.Post)).StatusCode);
        var after = await Read(f, token, "status=Pending&pageSize=1&page=2");
        Assert.Equal(1, after["page"]!.GetValue<int>()); Assert.Equal(1, after["totalCount"]!.GetValue<int>());
        await using var db = f.Context(); Assert.Equal(2, await db.LeaveRequests.CountAsync());
        Assert.Equal(2, await db.AuditEntries.CountAsync(a => a.Action == "Submit"));
        Assert.Single(await db.AuditEntries.Where(a => a.Action == "Cancel").ToArrayAsync());
    }

    /// <summary>The real credential/authorization/snapshot handler executes six SELECTs regardless of owned history size.</summary>
    [Theory]
    [InlineData(20)]
    [InlineData(2000)]
    public async Task BoundedCommandsAndSingleSnapshot(int size)
    {
        await using var f = await ApiFixture.StartAsync();
        await using (var seed = f.Context())
        {
            var type = await seed.LeaveTypes.Select(t => t.Id).SingleAsync();
            seed.LeaveRequests.AddRange(Enumerable.Range(0, size).Select(i => LeaveRequest.Create(f.Employee("employee").Id,
                type, new DateTime(2028, 1, 1), new DateTime(2028, 1, 2)))); await seed.SaveChangesAsync();
        }
        var counter = new CommandCounter();
        await using var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>()
            .UseSqlite("Data Source=" + f.DatabasePath + ";Foreign Keys=True").AddInterceptors(counter).Options);
        var access = new AccountAccessService(db); var actor = f.User("employee");
        var read = new SqliteTeamLeaveReadTransaction(db, new RequestCredentialValidator(new Credential(actor.Id.ToString(), actor.CredentialVersion.ToString()), access));
        var handler = new GetEmployeeLeaveHistoryQueryHandler(db, read,
            new CurrentAccountAuthorization(db, new EmployeeRoleLookupService(db), access), TimeProvider.System);
        var result = await handler.Handle(new() { ActorIdentityId = actor.Id }, default);
        Assert.Equal(size, result.TotalCount); Assert.Equal(Math.Min(size, 25), result.Items.Count);
        Assert.Equal(6, counter.Count); Assert.True(counter.AllProtected); Assert.False(db.ChangeTracker.HasChanges());
        Console.WriteLine($"history-query-count rows={size} selects={counter.Count} protected={counter.AllProtected}");
    }

    private sealed record Credential(string? IdentityUserId, string? Version) : IInitiatingCredential;
    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public bool AllProtected { get; private set; } = true;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Count++; AllProtected &= command.Transaction != null; return ValueTask.FromResult(result); }
    }
    private static async Task<JsonNode> Read(ApiFixture f, string token, string query)
    {
        using var response = await f.SendAsync(token, "leave-requests/history?" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await ApiFixture.JsonAsync(response);
    }
}
