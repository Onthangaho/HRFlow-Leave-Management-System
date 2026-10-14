using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using HRFlow.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace HRFlow.RegressionTests;

/// <summary>Exercises HTTP authorization and persisted outcomes against fresh migrated SQLite, never mocked EF storage.</summary>
public sealed class WorkflowTests
{
    /// <summary>Losing HR in a combined account removes administration without removing its independent personal/Manager capability.</summary>
    [Fact]
    public async Task CombinedRoleRemoval()
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("combined");
        await using var context = fixture.Context(); var role = await context.Roles.SingleAsync(r => r.Name == "HR Administrator");
        context.UserRoles.Remove(await context.UserRoles.SingleAsync(r => r.UserId == fixture.User("combined").Id && r.RoleId == role.Id)); await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.SendAsync(token, "employees")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(token, "leave-requests/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(token, "leave-requests/manager-attention")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(token, "me/image")).StatusCode);
    }

    /// <summary>A request entering through a valid Manager token while another writer holds protection must recheck current permission.</summary>
    [Fact]
    public async Task ManagerRevocationBehindWriter()
    {
        await using var fixture = await ApiFixture.StartAsync(); var id = await fixture.SubmitAsync("employee"); var token = await fixture.LoginAsync("manager");
        using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var entered = fixture.EnteredAsync("ApproveLeaveRequest"); var pending = fixture.SendAsync(token, $"leave-requests/{id}/approve", HttpMethod.Post, new { });
        await entered; using var command = connection.CreateCommand(); command.Transaction = writer;
        command.CommandText = "DELETE FROM AspNetUserRoles WHERE UserId=$user"; command.Parameters.AddWithValue("$user", fixture.User("manager").Id); command.ExecuteNonQuery(); writer.Commit();
        Assert.Equal(HttpStatusCode.Forbidden, (await pending).StatusCode); await AssertState(fixture, id, LeaveRequestStatus.Pending, 0);
    }

    /// <summary>Capabilities compose additively without granting HR decision or personal rights.</summary>
    [Theory]
    [InlineData("employee", 200, 403, 403)]
    [InlineData("manager", 200, 403, 200)]
    [InlineData("hr", 403, 200, 403)]
    [InlineData("combined", 200, 200, 200)]
    public async Task CapabilityMatrix(string actor, int personal, int administration, int team)
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync(actor);
        Assert.Equal(personal, (int)(await fixture.SendAsync(token, "leave-requests/dashboard")).StatusCode);
        Assert.Equal(administration, (int)(await fixture.SendAsync(token, "employees")).StatusCode);
        Assert.Equal(team, (int)(await fixture.SendAsync(token, "leave-requests/team-summary?start=2026-01-01&end=2026-01-31")).StatusCode);
    }

    /// <summary>Already-issued credentials cannot retain revoked capability or inactive/pending access.</summary>
    [Theory]
    [InlineData("manager", "role", 403)]
    [InlineData("manager", "inactive", 401)]
    [InlineData("manager", "pending", 401)]
    [InlineData("employee", "role", 403)]
    [InlineData("employee", "inactive", 401)]
    [InlineData("employee", "pending", 401)]
    [InlineData("hr", "role", 403)]
    [InlineData("hr", "inactive", 401)]
    [InlineData("hr", "pending", 401)]
    public async Task LiveAccessDenial(string actor, string change, int expected)
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync(actor);
        await using var context = fixture.Context();
        if (change == "role") { var id = fixture.User(actor).Id; context.UserRoles.RemoveRange(await context.UserRoles.Where(r => r.UserId == id).ToListAsync()); }
        if (change == "inactive") (await context.Employees.FindAsync(fixture.Employee(actor).Id))!.Deactivate(fixture.Employee("hr").Id, "Synthetic lifecycle");
        if (change == "pending") (await context.Users.FindAsync(fixture.User(actor).Id))!.RequiresActivation = true;
        await context.SaveChangesAsync();
        Assert.Equal(expected, (int)(await fixture.SendAsync(token, "me/image")).StatusCode);
        Assert.Equal(expected, (int)(await fixture.SendAsync(token, actor == "hr" ? "employees" : actor == "employee" ? "leave-requests/dashboard" : "leave-requests/team-summary?start=2026-01-01&end=2026-01-31")).StatusCode);
    }

    /// <summary>Relationship denials never persist a terminal state/audit, including self and cross-department rows.</summary>
    [Theory]
    [InlineData("hr", "employee")]
    [InlineData("manager", "manager")]
    [InlineData("manager", "unrelated")]
    [InlineData("manager", "cross")]
    public async Task DecisionScopeDenial(string actor, string owner)
    {
        await using var fixture = await ApiFixture.StartAsync(); var request = await fixture.PendingAsync(owner); var token = await fixture.LoginAsync(actor);
        foreach (var action in new[] { "approve", "reject" })
            Assert.Equal(HttpStatusCode.Forbidden, (await fixture.SendAsync(token, $"leave-requests/{request}/{action}", HttpMethod.Post, new { })).StatusCode);
        await AssertState(fixture, request, LeaveRequestStatus.Pending, 0);
    }

    /// <summary>Successful decisions/cancellation append exactly one immutable terminal transition; repeats cannot duplicate it.</summary>
    [Theory]
    [InlineData("approve", "manager", LeaveRequestStatus.Approved)]
    [InlineData("reject", "manager", LeaveRequestStatus.Rejected)]
    [InlineData("cancel", "employee", LeaveRequestStatus.Cancelled)]
    public async Task ExactlyOneAudit(string action, string actor, LeaveRequestStatus status)
    {
        await using var fixture = await ApiFixture.StartAsync(); var id = await fixture.SubmitAsync("employee"); var token = await fixture.LoginAsync(actor);
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.SendAsync(token, $"leave-requests/{id}/{action}", HttpMethod.Post, new { note = "Synthetic decision" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.SendAsync(token, $"leave-requests/{id}/{action}", HttpMethod.Post, new { })).StatusCode);
        await AssertState(fixture, id, status, 1);
        await using var db = fixture.Context(); var audit = await db.AuditEntries.SingleAsync(a => a.LeaveRequestId == id && a.OldStatus != null);
        Assert.Equal(fixture.Employee(actor).Id, audit.ActorId); Assert.Equal(LeaveRequestStatus.Pending, audit.OldStatus); Assert.Equal(status, audit.NewStatus);
        Assert.Single(await db.AuditEntries.Where(a => a.LeaveRequestId == id && a.OldStatus == null).ToListAsync());
    }

    /// <summary>Requests passing submission independently are checked again at approval against approved balance/overlap.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApprovalRevalidates(bool overlap)
    {
        await using var fixture = await ApiFixture.StartAsync(); var first = await fixture.SubmitAsync("employee", 1, overlap ? 3 : 15);
        var second = await fixture.SubmitAsync("employee", overlap ? 2 : 30, overlap ? 3 : 15); var token = await fixture.LoginAsync("manager");
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.SendAsync(token, $"leave-requests/{first}/approve", HttpMethod.Post, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.SendAsync(token, $"leave-requests/{second}/approve", HttpMethod.Post, new { })).StatusCode);
        await AssertState(fixture, second, LeaveRequestStatus.Pending, 0);
    }

    /// <summary>SQLite failure after audit insertion rolls back request status, audit and durable notification event together.</summary>
    [Fact]
    public async Task LateFailureRollsBack()
    {
        await using var fixture = await ApiFixture.StartAsync(); var id = await fixture.SubmitAsync("employee"); var token = await fixture.LoginAsync("manager");
        await using var db = fixture.Context(); var events = await db.LeaveNotificationEvents.CountAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER regression_failure AFTER INSERT ON AuditEntries WHEN NEW.Action='Approve' BEGIN SELECT RAISE(ABORT,'synthetic late persistence failure'); END;");
        try { Assert.Equal(HttpStatusCode.InternalServerError, (await fixture.SendAsync(token, $"leave-requests/{id}/approve", HttpMethod.Post, new { })).StatusCode); }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER regression_failure;"); }
        await AssertState(fixture, id, LeaveRequestStatus.Pending, 0); Assert.Equal(events, await db.LeaveNotificationEvents.CountAsync());
    }

    /// <summary>Two independent API processes competing across requests cannot overconsume an employee's balance.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrossProcessBalance(bool overlap)
    {
        await using var fixture = await ApiFixture.StartAsync(); var first = await fixture.SubmitAsync("employee", 1, overlap ? 3 : 15); var second = await fixture.SubmitAsync("employee", overlap ? 2 : 30, overlap ? 3 : 15);
        var token = await fixture.LoginAsync("manager"); using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var entered = fixture.EnteredAsync("ApproveLeaveRequest", 2);
        var a = fixture.SendAsync(token, $"leave-requests/{first}/approve", HttpMethod.Post, new { }, 0);
        var b = fixture.SendAsync(token, $"leave-requests/{second}/approve", HttpMethod.Post, new { }, 1);
        await entered; writer.Commit(); var results = await Task.WhenAll(a, b);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(results, r => r.StatusCode == HttpStatusCode.BadRequest);
        await using var db = fixture.Context(); var approved = await db.LeaveRequests.Where(r => r.Status == LeaveRequestStatus.Approved).ToListAsync();
        Assert.Single(approved); Assert.Equal(overlap ? 3 : 15, approved.Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1));
        Assert.Single(await db.AuditEntries.Where(r => r.Action == "Approve").ToListAsync());
    }

    /// <summary>Same-request approve/reject across API processes commits one status and exactly one corresponding audit.</summary>
    [Fact]
    public async Task CrossProcessSameRequest()
    {
        await using var fixture = await ApiFixture.StartAsync(); var id = await fixture.SubmitAsync("employee"); var token = await fixture.LoginAsync("manager");
        using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var enteredA = fixture.EnteredAsync("ApproveLeaveRequest"); var enteredB = fixture.EnteredAsync("RejectLeaveRequest");
        var a = fixture.SendAsync(token, $"leave-requests/{id}/approve", HttpMethod.Post, new { }, 0);
        var b = fixture.SendAsync(token, $"leave-requests/{id}/reject", HttpMethod.Post, new { }, 1);
        await Task.WhenAll(enteredA, enteredB); writer.Commit(); var results = await Task.WhenAll(a, b);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.NoContent); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.Context(); var request = await db.LeaveRequests.FindAsync(id); Assert.NotEqual(LeaveRequestStatus.Pending, request!.Status);
        var audit = Assert.Single(await db.AuditEntries.Where(r => r.LeaveRequestId == id && r.OldStatus != null).ToListAsync()); Assert.Equal(request.Status, audit.NewStatus);
    }

    /// <summary>Current HR permission is checked after a blocked write obtains the reservation, not only at the API gate.</summary>
    [Fact]
    public async Task RevocationBehindWriter()
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("hr");
        using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var entered = fixture.EnteredAsync("CreateEmployee"); var pending = fixture.SendAsync(token, "employees", HttpMethod.Post, fixture.Creation("blocked@synthetic.invalid", "BLOCKED"));
        await entered; using var command = connection.CreateCommand(); command.Transaction = writer;
        command.CommandText = "DELETE FROM AspNetUserRoles WHERE UserId=$user"; command.Parameters.AddWithValue("$user", fixture.User("hr").Id); command.ExecuteNonQuery(); writer.Commit();
        Assert.Equal(HttpStatusCode.Forbidden, (await pending).StatusCode);
        await using var db = fixture.Context(); Assert.False(await db.Employees.AnyAsync(e => e.Email == "blocked@synthetic.invalid")); Assert.False(await db.Users.AnyAsync(u => u.Email == "blocked@synthetic.invalid"));
    }

    /// <summary>Profile/Identity replacements retain original expectedVersion rather than silently overwriting newer records.</summary>
    [Fact]
    public async Task StaleEmployeeEdit()
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("hr"); var owner = fixture.Employee("employee");
        using var response = await fixture.SendAsync(token, $"employees/{owner.Id}"); var dto = await ApiFixture.JsonAsync(response);
        object Edit(string name) => new { fullName = name, email = owner.Email, departmentId = owner.DepartmentId, roles = new[] { "Employee" }, expectedVersion = dto["version"]!.GetValue<Guid>(), managerAssignment = "Preserve" };
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(token, $"employees/{owner.Id}", HttpMethod.Put, Edit("New synthetic name"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.SendAsync(token, $"employees/{owner.Id}", HttpMethod.Put, Edit("Stale synthetic name"))).StatusCode);
        await using var db = fixture.Context(); Assert.Equal("New synthetic name", (await db.Employees.FindAsync(owner.Id))!.FullName); Assert.Equal(owner.ManagerId, (await db.Employees.FindAsync(owner.Id))!.ManagerId);
    }

    /// <summary>Shared policy edits cannot overwrite fresh configuration using an old loaded version.</summary>
    [Fact]
    public async Task StalePolicyEdit()
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("hr");
        var policies = await ApiFixture.JsonAsync(await fixture.SendAsync(token, "management/leave-policies")); var policy = policies.AsArray()[0]!;
        var id = policy["id"]!.GetValue<Guid>(); object Edit(int balance) => new { name = "Synthetic policy", allowOverlap = false, defaultBalance = balance, expectedVersion = policy["version"]!.GetValue<Guid>() };
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(token, $"management/leave-policies/{id}", HttpMethod.Put, Edit(25))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.SendAsync(token, $"management/leave-policies/{id}", HttpMethod.Put, Edit(99))).StatusCode);
        await using var db = fixture.Context(); Assert.Equal(25, (await db.LeavePolicies.FindAsync(id))!.DefaultBalance);
    }

    /// <summary>Concurrent normalized email/number collisions use separate API processes and leave no orphan accounts.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CrossProcessDuplicates(bool emailDuplicate)
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("hr");
        using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var entered = fixture.EnteredAsync("CreateEmployee", 2);
        var a = fixture.SendAsync(token, "employees", HttpMethod.Post, fixture.Creation("import@synthetic.invalid", "NUMBER-1"), 0);
        var b = fixture.SendAsync(token, "employees", HttpMethod.Post, fixture.Creation(emailDuplicate ? "IMPORT@SYNTHETIC.INVALID" : "different@synthetic.invalid", emailDuplicate ? "NUMBER-2" : " number-1 "), 1);
        await entered; writer.Commit(); var results = await Task.WhenAll(a, b);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.Context(); Assert.Single(await db.Employees.Where(e => e.EmployeeNumber == "NUMBER-1" || e.EmployeeNumber == "NUMBER-2").ToListAsync());
        Assert.Equal(await db.Employees.CountAsync(), await db.Users.CountAsync());
    }

    /// <summary>Repeated concurrent CSV confirmation uses server-held rows and commits one account plus durable outcome.</summary>
    [Fact]
    public async Task RepeatedCsvConfirmation()
    {
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("hr");
        using var content = new MultipartFormDataContent();
        var columns = new[] { "FirstName", "LastName", "EmailAddress", "EmployeeNumber", "Roles", "Department", "EmploymentStartDate" };
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(string.Join(',', columns) + "\nSynthetic,Import,csv@synthetic.invalid,CSV-1,Employee,Synthetic Engineering,2020-01-01\n")), "file", "synthetic.csv");
        content.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(columns.ToDictionary(c => c, c => c))), "mapping");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) }; var state = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(fixture.BrowserState))!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.PostAsync(state["api"]!.GetValue<string>() + "/api/v1/onboarding/previews", content); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var preview = await ApiFixture.JsonAsync(response); var id = preview["id"]!.GetValue<Guid>(); Assert.Equal("Ready", preview["rows"]![0]!["state"]!.GetValue<string>());
        var body = new { expectedVersion = preview["version"]!.GetValue<Guid>(), idempotencyKey = Guid.NewGuid().ToString(), rows = new[] { 2 } };
        using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var entered = fixture.EnteredAsync("Confirm", 2);
        var a = fixture.SendAsync(token, $"onboarding/previews/{id}/confirm", HttpMethod.Post, body, 0);
        var b = fixture.SendAsync(token, $"onboarding/previews/{id}/confirm", HttpMethod.Post, body, 1);
        await entered; writer.Commit(); var results = await Task.WhenAll(a, b);
        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(HttpStatusCode.OK, (await fixture.SendAsync(token, $"onboarding/previews/{id}/confirm", HttpMethod.Post, body)).StatusCode);
        await using var db = fixture.Context(); Assert.Single(await db.Users.Where(u => u.NormalizedEmail == "CSV@SYNTHETIC.INVALID").ToListAsync());
        Assert.Single(await db.Employees.Where(e => e.EmployeeNumber == "CSV-1").ToListAsync());
        var batch = await db.OnboardingBatches.FindAsync(id); Assert.Equal(body.idempotencyKey, batch!.ConfirmationKey);
        var rows = JsonNode.Parse(batch.RowsJson)!.AsArray(); Assert.Single(rows); Assert.Equal("Created", rows[0]!["state"]!.GetValue<string>());
    }

    private static async Task AssertState(ApiFixture fixture, Guid id, LeaveRequestStatus expected, int terminalAudits)
    {
        await using var context = fixture.Context(); Assert.Equal(expected, (await context.LeaveRequests.FindAsync(id))!.Status);
        Assert.Equal(terminalAudits, await context.AuditEntries.CountAsync(a => a.LeaveRequestId == id && a.OldStatus != null));
    }
}
