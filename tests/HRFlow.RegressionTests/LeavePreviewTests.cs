using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Actual authenticated HTTP preview reconciled against migrated SQLite and the existing write workflow.</summary>
public sealed class LeavePreviewTests
{
    /// <summary>Current capabilities remain additive; HR alone has no owner preview.</summary>
    [Theory]
    [InlineData("employee", 200)]
    [InlineData("manager", 200)]
    [InlineData("combined", 200)]
    [InlineData("hr", 403)]
    public async Task CapabilitiesAndReadOnlyOwnerScope(string actor, int expected)
    {
        await using var f = await ApiFixture.StartAsync();
        var token = await f.LoginAsync(actor); var type = await Selected(f, token, actor == "hr");
        var before = Fingerprint(f);
        using var response = await f.SendAsync(token, "leave-requests/preview", HttpMethod.Post,
            new { leaveTypeId = type["id"]!.GetValue<Guid>(), startDate = "2028-02-28", endDate = "2028-03-01", employeeId = f.Employee("unrelated").Id });
        Assert.Equal(expected, (int)response.StatusCode);
        if (expected == 200)
        {
            var data = await ApiFixture.JsonAsync(response);
            Assert.Equal(3, data["requestedDays"]!.GetValue<int>());
            Assert.Equal(20, data["entitledDays"]!.GetValue<int>());
            Assert.Equal(0, data["approvedUsedDays"]!.GetValue<int>());
            Assert.Equal(17, data["projectedRemainingDaysIfApproved"]!.GetValue<int>());
            Assert.Empty(data["findings"]!.AsArray());
            Assert.Equal(type["version"]!.GetValue<Guid>(), data["requirements"]!["typeVersion"]!.GetValue<Guid>());
            Assert.True(DateTimeOffset.Parse(data["capturedAtUtc"]!.GetValue<string>(), CultureInfo.InvariantCulture) <= DateTimeOffset.UtcNow);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        }
        Assert.Equal(before, Fingerprint(f));
    }

    /// <summary>Calendar units include weekends/leap days; invalid dates return findings without invented numeric durations.</summary>
    [Theory]
    [InlineData("2028-02-29", "2028-02-29", 1, null)]
    [InlineData("2028-03-04", "2028-03-05", 2, null)]
    [InlineData("2028-03-05", "2028-03-04", null, "EndDate")]
    [InlineData("2020-01-01", "2020-01-01", 1, "StartDate")]
    public async Task Dates(string start, string end, int? days, string? finding)
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("employee"); var type = await Selected(f, token);
        using var response = await Preview(f, token, type, start, end);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); var data = await ApiFixture.JsonAsync(response);
        Assert.Equal(days, data["requestedDays"]?.GetValue<int>());
        if (finding == null) Assert.Empty(data["findings"]!.AsArray());
        else Assert.Contains(data["findings"]!.AsArray(), p => p!["code"]!.GetValue<string>() == finding);
    }

    /// <summary>Preview shares Approved usage, Pending non-reservation and same-type overlap semantics with actual submission/approval.</summary>
    [Fact]
    public async Task ReconcilesAndSubmissionStillRevalidates()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee"); var manager = await f.LoginAsync("manager");
        var type = await Selected(f, owner); var start = DateTime.UtcNow.Date.AddDays(20);
        using var initial = await Preview(f, owner, type, Date(start), Date(start.AddDays(2)));
        var id = await Submit(f, owner, type, start, start.AddDays(2));
        using var pending = await Preview(f, owner, type, Date(start), Date(start.AddDays(2)));
        Assert.Empty((await ApiFixture.JsonAsync(pending))["findings"]!.AsArray());
        Assert.Equal(HttpStatusCode.NoContent, (await f.SendAsync(manager, $"leave-requests/{id}/approve", HttpMethod.Post, new { })).StatusCode);
        // Await the durable acknowledgement, not a guessed delay: legitimate delivery must not be attributed to preview by the all-table comparison.
        using (var ready = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        await using (var settled = f.Context())
        {
            Assert.Equal(2, await settled.LeaveNotificationEvents.CountAsync(ready.Token));
            while (await settled.LeaveNotificationEvents.AsNoTracking().AnyAsync(e => e.DeliveredAtUtc == null, ready.Token))
                await Task.Delay(25, ready.Token); // Bounded readiness polling on persisted state, not concurrency ordering by sleep.
        }
        var before = Fingerprint(f);
        using var overlap = await Preview(f, owner, type, Date(start), Date(start.AddDays(2)));
        var data = await ApiFixture.JsonAsync(overlap);
        Assert.Equal(3, data["approvedUsedDays"]!.GetValue<int>());
        Assert.Equal(17, data["remainingDays"]!.GetValue<int>());
        Assert.Contains(data["findings"]!.AsArray(), p => p!["code"]!.GetValue<string>() == "ApprovedOverlap");
        using var balances = await f.SendAsync(owner, "leave-requests/balances");
        var balance = (await ApiFixture.JsonAsync(balances)).AsArray().Single(t => t!["leaveTypeId"]!.GetValue<Guid>() == type["id"]!.GetValue<Guid>())!;
        Assert.Equal(balance["remainingDays"]!.GetValue<int>(), data["remainingDays"]!.GetValue<int>());
        Assert.Equal(before, Fingerprint(f));
        // The earlier positive preview does not authorize this now-overlapping submission.
        using var denied = await f.SendAsync(owner, "leave-requests", HttpMethod.Post, Body(type, start, start.AddDays(2)));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await using var context = f.Context();
        Assert.Equal(1, await context.LeaveRequests.CountAsync());
        Assert.Equal(2, await context.AuditEntries.CountAsync());
        var policy = await context.LeavePolicies.SingleAsync(); policy.Update(policy.Name, true, 0); await context.SaveChangesAsync();
        using var changed = await f.SendAsync(owner, "leave-requests", HttpMethod.Post, Body(type, start.AddDays(10), start.AddDays(10)));
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        using var exhausted = await Preview(f, owner, type, Date(start.AddDays(10)), Date(start.AddDays(10)));
        var updated = await ApiFixture.JsonAsync(exhausted);
        Assert.Equal(-3, updated["remainingDays"]!.GetValue<int>());
        Assert.Contains(updated["findings"]!.AsArray(), p => p!["code"]!.GetValue<string>() == "InsufficientBalance");
        Assert.NotEqual(type["policyVersion"]!.GetValue<Guid>(), updated["requirements"]!["policyVersion"]!.GetValue<Guid>());
    }

    /// <summary>Manager problems are correctable findings; account/role/credential revocation stays an HTTP denial.</summary>
    [Theory]
    [InlineData("manager", 200)]
    [InlineData("role", 403)]
    [InlineData("inactive", 401)]
    [InlineData("pending", 401)]
    [InlineData("credential", 401)]
    public async Task CurrentAccess(string change, int expected)
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("employee"); var type = await Selected(f, token);
        await using (var c = f.Context())
        {
            if (change == "manager") (await c.Employees.FindAsync(f.Employee("employee").Id))!.AssignManager(null);
            if (change == "role") c.UserRoles.RemoveRange(await c.UserRoles.Where(r => r.UserId == f.User("employee").Id).ToListAsync());
            if (change == "inactive") (await c.Employees.FindAsync(f.Employee("employee").Id))!.Deactivate(f.Employee("hr").Id, "Synthetic");
            if (change == "pending") (await c.Users.FindAsync(f.User("employee").Id))!.RequiresActivation = true;
            if (change == "credential") (await c.Users.FindAsync(f.User("employee").Id))!.CredentialVersion = Guid.NewGuid();
            await c.SaveChangesAsync();
        }
        using var response = await Preview(f, token, type, "2028-02-29", "2028-02-29");
        Assert.Equal(expected, (int)response.StatusCode);
        if (expected == 200) Assert.Contains((await ApiFixture.JsonAsync(response))["findings"]!.AsArray(), p => p!["code"]!.GetValue<string>() == "ManagerAssignment");
    }

    /// <summary>Malformed dates/identifiers and oversized bodies are safe client errors, not infrastructure failures.</summary>
    [Fact]
    public async Task BoundedInputs()
    {
        await using var f = await ApiFixture.StartAsync(); var token = await f.LoginAsync("employee");
        foreach (var body in new object[] {
            new { leaveTypeId = Guid.Empty, startDate = "2028-01-01", endDate = "2028-01-01" },
            new { leaveTypeId = Guid.NewGuid(), startDate = "2028-02-30", endDate = "2028-03-01" },
            new { leaveTypeId = Guid.NewGuid(), startDate = "2028-01-01", endDate = "2028-01-01", extra = new string('x', 3000) } })
        {
            using var response = await f.SendAsync(token, "leave-requests/preview", HttpMethod.Post, body);
            Assert.InRange((int)response.StatusCode, 400, 499);
        }
    }

    /// <summary>A separate process can finish preview while another connection owns the writer reservation; preview never queues a business write.</summary>
    [Fact]
    public async Task DeferredReadDoesNotReserveWriterAndLaterPermissionStillApplies()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee"); var type = await Selected(f, owner);
        using (var connection = f.Connection())
        using (var writer = connection.BeginTransaction(deferred: false))
        {
            using var preview = await Preview(f, owner, type, "2028-02-29", "2028-02-29");
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            using var command = connection.CreateCommand(); command.Transaction = writer;
            command.CommandText = "DELETE FROM AspNetUserRoles WHERE UserId=$user";
            command.Parameters.AddWithValue("$user", f.User("employee").Id); command.ExecuteNonQuery(); writer.Commit();
        }
        using var submit = await f.SendAsync(owner, "leave-requests", HttpMethod.Post, Body(type, new DateTime(2028, 2, 29), new DateTime(2028, 2, 29)));
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
        await using var context = f.Context(); Assert.Equal(0, await context.LeaveRequests.CountAsync()); Assert.Equal(0, await context.AuditEntries.CountAsync());
    }

    /// <summary>Zero and exactly exhausted balances are finite, while an explicitly overlap-enabled policy retains its existing semantics.</summary>
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(2, 2, false)]
    [InlineData(20, 2, true)]
    public async Task ZeroExhaustedAndAllowedOverlap(int entitlement, int used, bool overlap)
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee"); var type = await Selected(f, owner);
        await using (var c = f.Context())
        {
            var policy = await c.LeavePolicies.SingleAsync(); policy.Update(policy.Name, overlap, entitlement);
            if (used != 0)
            {
                var r = LeaveRequest.Create(f.Employee("employee").Id, type["id"]!.GetValue<Guid>(), new DateTime(2028, 2, 28), new DateTime(2028, 2, 29));
                r.Approve(f.Employee("manager").Id); c.LeaveRequests.Add(r);
            }
            await c.SaveChangesAsync();
        }
        var before = Fingerprint(f);
        using var response = await Preview(f, owner, type, "2028-02-29", "2028-02-29");
        var data = await ApiFixture.JsonAsync(response);
        Assert.Equal(entitlement - used, data["remainingDays"]!.GetValue<int>());
        Assert.Equal(entitlement - used - 1, data["projectedRemainingDaysIfApproved"]!.GetValue<int>());
        Assert.Equal(entitlement <= used, data["findings"]!.AsArray().Any(p => p!["code"]!.GetValue<string>() == "InsufficientBalance"));
        Assert.Equal(used > 0 && !overlap, data["findings"]!.AsArray().Any(p => p!["code"]!.GetValue<string>() == "ApprovedOverlap"));
        Assert.Equal(before, Fingerprint(f));
    }

    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static async Task<JsonNode> Selected(ApiFixture f, string token, bool hr = false)
    {
        if (hr) token = await f.LoginAsync("employee");
        using var response = await f.SendAsync(token, "leave-types"); return (await ApiFixture.JsonAsync(response)).AsArray().Single()!;
    }
    private static Task<HttpResponseMessage> Preview(ApiFixture f, string token, JsonNode type, string start, string end) =>
        f.SendAsync(token, "leave-requests/preview", HttpMethod.Post, new { leaveTypeId = type["id"]!.GetValue<Guid>(), startDate = start, endDate = end });
    private static object Body(JsonNode type, DateTime start, DateTime end) => new { leaveTypeId = type["id"]!.GetValue<Guid>(), startDate = start, endDate = end,
        expectedTypeVersion = type["version"]!.GetValue<Guid>(), expectedPolicyVersion = type["policyVersion"]!.GetValue<Guid>() };
    private static async Task<Guid> Submit(ApiFixture f, string token, JsonNode type, DateTime start, DateTime end)
    {
        using var response = await f.SendAsync(token, "leave-requests", HttpMethod.Post, Body(type, start, end));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await ApiFixture.JsonAsync(response)).GetValue<Guid>();
    }
    // Compare all persisted tables, not just request counts; no row values or private fixture locations are reported.
    private static string Fingerprint(ApiFixture f)
    {
        using var connection = f.Connection(); using var tables = connection.CreateCommand();
        tables.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
        var names = new List<string>(); using (var reader = tables.ExecuteReader()) while (reader.Read()) names.Add(reader.GetString(0));
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var name in names)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + name.Replace("\"", "\"\"") + "\" ORDER BY rowid";
            using var reader = command.ExecuteReader();
            while (reader.Read()) for (var i = 0; i < reader.FieldCount; i++)
                digest.AppendData(Encoding.UTF8.GetBytes(Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) + "\0"));
        }
        return Convert.ToHexString(digest.GetHashAndReset());
    }
}
