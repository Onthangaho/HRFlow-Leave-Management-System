using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Exercises actual HTTP, migrated SQLite and persisted history for the bounded read-only HR projection.</summary>
public sealed class HrMonitoringTests
{
    private const string Route = "leave-requests/monitoring/pending";

    /// <summary>Search/intersection/type/current-department filters precede stable paging and never alter business rows.</summary>
    [Fact]
    public async Task FilteredPagesPreserveScopeOrderingAndHistory()
    {
        await using var f = await ApiFixture.StartAsync();
        await using var db = f.Context();
        var type = await db.LeaveTypes.SingleAsync(); var policy = await db.LeavePolicies.SingleAsync();
        var otherType = LeaveType.Create("Synthetic second type", policy); db.LeaveTypes.Add(otherType);
        for (var i = 0; i < 55; i++) db.LeaveRequests.Add(LeaveRequest.Create(f.Employee("employee").Id, type.Id,
            new DateTime(2028, 2, 28).AddDays(i / 20), new(2028, 3, 1)));
        db.LeaveRequests.Add(LeaveRequest.Create(f.Employee("cross").Id, type.Id, new(2028, 2, 29), new(2028, 2, 29)));
        db.LeaveRequests.Add(LeaveRequest.Create(f.Employee("employee").Id, otherType.Id, new(2028, 2, 29), new(2028, 2, 29)));
        await db.SaveChangesAsync();
        await db.Employees.Where(e => e.Id == f.Employee("employee").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.FullName, "Monitoring %_ Owner"));
        var before = await Facts(f); var token = await f.LoginAsync("hr");
        var first = await Read(f, token); var second = await Read(f, token, "page=2"); var third = await Read(f, token, "page=3");
        Assert.Equal(57, first["totalCount"]!.GetValue<int>()); Assert.Equal(25, first["items"]!.AsArray().Count);
        var ids = new[] { first, second, third }.SelectMany(p => p["items"]!.AsArray()).Select(i => i!["id"]!.GetValue<Guid>()).ToArray();
        var expected = await db.LeaveRequests.OrderBy(r => r.StartDate).ThenBy(r => r.Id).Select(r => r.Id).ToArrayAsync();
        Assert.Equal(expected, ids); Assert.Equal(57, ids.Distinct().Count());
        Assert.Equal(3, (await Read(f, token, "page=2147483647"))["page"]!.GetValue<int>());
        Assert.Equal(57, (await Read(f, token, "pageSize=100"))["items"]!.AsArray().Count);
        Assert.Equal(56, Count(await Read(f, token, "search=" + Uri.EscapeDataString("%_"))));
        Assert.Equal(56, Count(await Read(f, token, "search=" + Uri.EscapeDataString(f.Employee("employee").Email.ToUpperInvariant()))));
        var combined = $"search={Uri.EscapeDataString("%_")}&departmentId={f.Employee("employee").DepartmentId}&leaveTypeId={type.Id}&start=2028-02-29&end=2028-02-29";
        Assert.Equal(40, Count(await Read(f, token, combined)));
        Assert.Equal(55, Count(await Read(f, token, combined.Replace("start=2028-02-29&end=2028-02-29", "start=2028-03-01&end=2028-03-01"))));
        Assert.Equal(0, Count(await Read(f, token, "end=2028-02-27")));
        Assert.Equal(0, Count(await Read(f, token, "start=2028-03-02")));
        Assert.Equal(0, Count(await Read(f, token, "departmentId=" + Guid.NewGuid())));
        Assert.Equal(0, Count(await Read(f, token, "leaveTypeId=" + Guid.NewGuid())));
        Assert.Equal(0, Count(await Read(f, token, "search=unmatched-synthetic")));
        var keys = first["items"]![0]!.AsObject().Select(p => p.Key).Order().ToArray();
        Assert.Equal(new[] { "employeeEmail", "employeeFullName", "employeeId", "endDate", "id", "leaveTypeId", "leaveTypeName", "startDate", "status" }, keys);
        Assert.NotEqual(default, first["capturedAtUtc"]!.GetValue<DateTimeOffset>());
        using var workspace = await f.SendAsync(token, "reports/hr-workspace?start=2028-02-01&end=2028-03-01");
        // The separate operational total is independently reconciled below without using displayed page length.
        Assert.Equal(HttpStatusCode.OK, workspace.StatusCode);
        Assert.Equal(57, (await ApiFixture.JsonAsync(workspace))["pendingCount"]!.GetValue<int>());
        Assert.Equal(before, await Facts(f));
        await db.Employees.Where(e => e.Id == f.Employee("employee").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.DepartmentId, f.Employee("cross").DepartmentId));
        Assert.Equal(0, Count(await Read(f, token, "departmentId=" + f.Employee("employee").DepartmentId)));
        Assert.Equal(57, Count(await Read(f, token, "departmentId=" + f.Employee("cross").DepartmentId)));
    }

    /// <summary>Only current HR capabilities grant monitoring, independently of personal or Manager membership.</summary>
    [Fact]
    public async Task CurrentAuthorizationAndCredentialRevocation()
    {
        await using var f = await ApiFixture.StartAsync();
        foreach (var actor in new[] { "employee", "manager" })
            Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(await f.LoginAsync(actor), Route)).StatusCode);
        var hr = await f.LoginAsync("hr"); var combined = await f.LoginAsync("combined");
        Assert.Equal(0, Count(await Read(f, hr))); Assert.Equal(0, Count(await Read(f, combined)));
        await using var db = f.Context(); var role = await db.Roles.SingleAsync(r => r.Name == "HR Administrator");
        await db.UserRoles.Where(r => r.UserId == f.User("combined").Id && r.RoleId == role.Id).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(combined, Route)).StatusCode);
        var rotatedVersion = Guid.NewGuid();
        await db.Users.Where(u => u.Id == f.User("hr").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CredentialVersion, rotatedVersion));
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(hr, Route)).StatusCode);
        Assert.Equal(0, await db.LeaveRequests.CountAsync()); Assert.Equal(0, await db.AuditEntries.CountAsync());
    }

    /// <summary>Already-issued HR tokens cannot retain inactive or pending-account monitoring access.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LifecycleDenial(bool pending)
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("hr");
        await using var db = f.Context();
        if (pending) await db.Users.Where(u => u.Id == f.User("hr").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.RequiresActivation, true));
        else { (await db.Employees.FindAsync(f.Employee("hr").Id))!.Deactivate(f.Employee("combined").Id, "Synthetic lifecycle"); await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.SendAsync(token, Route)).StatusCode);
    }

    /// <summary>Invalid filters fail safely; reads after a real decision clamp empty pages without generating audits.</summary>
    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task BoundsAndFinalPageAfterDecision(string action)
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("hr");
        var empty = await Read(f, token, "page=2147483647"); Assert.Equal(1, empty["page"]!.GetValue<int>()); Assert.Equal(1, empty["totalPages"]!.GetValue<int>());
        foreach (var invalid in new[] { "page=0", "page=-1", "page=2147483648", "pageSize=101", "pageSize=0", "departmentId=bad", "leaveTypeId=" + Guid.Empty,
            "departmentId=" + Guid.Empty, "start=2028-02-30", "start=2028-03-01&end=2028-02-29", "search=" + new string('x', 101) })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.SendAsync(token, Route + "?" + invalid)).StatusCode);
        var id = await f.SubmitAsync("employee");
        Assert.Equal(1, Count(await Read(f, token)));
        Assert.Equal(HttpStatusCode.Forbidden, (await f.SendAsync(token, $"leave-requests/{id}/{action}", HttpMethod.Post, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await f.SendAsync(await f.LoginAsync("manager"), $"leave-requests/{id}/{action}", HttpMethod.Post, new { })).StatusCode);
        var before = await Facts(f);
        var last = await Read(f, token, "page=2147483647");
        Assert.Equal(0, Count(last)); Assert.Equal(1, last["page"]!.GetValue<int>()); Assert.Empty(last["items"]!.AsArray());
        Assert.Equal(before, await Facts(f));
    }

    private static int Count(JsonNode page) => page["totalCount"]!.GetValue<int>();
    private static async Task<JsonNode> Read(ApiFixture f, string token, string filters = "")
    {
        using var response = await f.SendAsync(token, Route + "?" + filters);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        return await ApiFixture.JsonAsync(response);
    }
    private static async Task<string> Facts(ApiFixture f)
    {
        await using var db = f.Context();
        return JsonSerializer.Serialize(new {
            Requests = await db.LeaveRequests.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Id, r.Status, r.StartDate, r.EndDate, r.Description, r.SubmissionRequirements }).ToArrayAsync(),
            Audits = await db.AuditEntries.AsNoTracking().OrderBy(a => a.Id).Select(a => new { a.Id, a.Action, a.Timestamp, a.NewStatus, a.DecisionNote }).ToArrayAsync(),
            Events = await db.LeaveNotificationEvents.CountAsync(), Documents = await db.SupportingDocuments.CountAsync()
        });
    }
}
