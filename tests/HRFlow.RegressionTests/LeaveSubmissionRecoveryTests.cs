using System.Net;
using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Exercises actual HTTP, two API processes and persisted outcomes; never relies on response status alone for atomicity.</summary>
public sealed class LeaveSubmissionRecoveryTests
{
    private sealed record Attempt(Guid SubmissionKey, Guid LeaveTypeId, DateTime StartDate, DateTime EndDate,
        Guid ExpectedTypeVersion, Guid ExpectedPolicyVersion, string? Description, Guid[] DocumentIds);

    private static async Task<Attempt> Input(ApiFixture f, params Guid[] documents)
    {
        await using var db = f.Context(); var type = await db.LeaveTypes.Include(t => t.LeavePolicy).SingleAsync();
        return new(Guid.NewGuid(), type.Id, DateTime.UtcNow.Date.AddDays(30), DateTime.UtcNow.Date.AddDays(31),
            type.Version, type.LeavePolicy.Version, null, documents);
    }
    private static Task<HttpResponseMessage> Submit(ApiFixture f, string actor, Attempt input, int server = 0) => f.SendAsync(actor, "leave-requests", HttpMethod.Post, input, server);
    private static Task<HttpResponseMessage> Check(ApiFixture f, string actor, Attempt input) => f.SendAsync(actor, "leave-requests/submission-outcome", HttpMethod.Post, input);
    private static async Task AssertOne(ApiFixture f, Guid request, int documents = 0)
    {
        await using var db = f.Context();
        Assert.Single(await db.LeaveRequests.ToArrayAsync()); Assert.Single(await db.LeaveSubmissionOutcomes.ToArrayAsync());
        Assert.Single(await db.AuditEntries.Where(a => a.LeaveRequestId == request && a.Action == "Submit").ToArrayAsync());
        Assert.Single(await db.LeaveNotificationEvents.Where(e => e.RequestId == request).ToArrayAsync());
        Assert.Equal(documents, await db.SupportingDocuments.CountAsync(d => d.RequestId == request));
        var outcome = await db.LeaveSubmissionOutcomes.SingleAsync(); Assert.Equal(request, outcome.RequestId); Assert.Equal(64, outcome.PayloadHash.Length);
    }

    /// <summary>An unread original body and explicit read-only check recover one committed request with bound evidence and immutable snapshots.</summary>
    [Theory]
    [InlineData("Approve")]
    [InlineData("Reject")]
    [InlineData("Cancel")]
    public async Task LostOutcomeRecoversAfterTransitionAndConfigurationChange(string transition)
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee");
        var draft = await EvidenceAdditionTests.DraftAsync(f); var input = await Input(f, draft.Id);
        using (var original = await Submit(f, owner, input)) Assert.Equal(HttpStatusCode.OK, original.StatusCode); // Deliberately discard body; browser gate drops transport after commit.
        Guid id; RequestRequirementsSnapshot? snapshot;
        await using (var db = f.Context()) { var row = await db.LeaveRequests.SingleAsync(); id = row.Id; snapshot = row.SubmissionRequirements; }
        var actor = transition == "Cancel" ? owner : await f.LoginAsync("manager");
        using (var change = await f.SendAsync(actor, $"leave-requests/{id}/{transition.ToLowerInvariant()}", HttpMethod.Post, new { })) Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        await using (var db = f.Context())
        {
            var policy = await db.LeavePolicies.SingleAsync(); policy.Update(policy.Name, false, 0);
            var type = await db.LeaveTypes.SingleAsync(); type.ConfigureRequirements("Required", "NotRequested", "Medical", "Later synthetic rule"); await db.SaveChangesAsync();
        }
        using var checkedResult = await Check(f, owner, input); Assert.Equal(HttpStatusCode.OK, checkedResult.StatusCode);
        Assert.Equal(id, (await ApiFixture.JsonAsync(checkedResult))["requestId"]!.GetValue<Guid>());
        Assert.Equal("no-store", checkedResult.Headers.CacheControl!.ToString());
        using var recovered = await Submit(f, owner, input, 1); Assert.Equal(HttpStatusCode.OK, recovered.StatusCode); Assert.Equal(id, (await ApiFixture.JsonAsync(recovered)).GetValue<Guid>());
        await using var state = f.Context(); Assert.Single(await state.LeaveRequests.ToArrayAsync()); Assert.Single(await state.LeaveSubmissionOutcomes.ToArrayAsync());
        Assert.Equal(2, await state.AuditEntries.CountAsync()); Assert.Equal(snapshot, (await state.LeaveRequests.SingleAsync()).SubmissionRequirements);
        Assert.Equal(id, (await state.SupportingDocuments.SingleAsync()).RequestId); Assert.Equal(2, await state.LeaveNotificationEvents.CountAsync());
    }

    /// <summary>A held database writer and endpoint-entry barrier coordinate genuinely separate API processes.</summary>
    [Fact]
    public async Task ConcurrentIdenticalAttemptsCommitExactlyOnce()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee");
        var draft = await EvidenceAdditionTests.DraftAsync(f); var input = await Input(f, draft.Id);
        using var connection = f.Connection(); using var writer = connection.BeginTransaction(deferred: false);
        var entered = f.EnteredAsync("LeaveRequestsController.SubmitLeaveRequest", 2);
        var first = Submit(f, owner, input, 0); var second = Submit(f, owner, input, 1); await entered; writer.Commit();
        var results = await Task.WhenAll(first, second);
        try
        {
            Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
            var id = (await ApiFixture.JsonAsync(results[0])).GetValue<Guid>(); Assert.Equal(id, (await ApiFixture.JsonAsync(results[1])).GetValue<Guid>());
            await AssertOne(f, id, 1);
        }
        finally { foreach (var result in results) result.Dispose(); }
    }

    /// <summary>Document ordering and trimmed description are canonical; each changed business field conflicts.</summary>
    [Fact]
    public async Task CanonicalIdentityAndIndependentKeys()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee");
        await using (var db = f.Context()) { (await db.LeaveTypes.SingleAsync()).ConfigureRequirements("Optional", "Optional", "Ordinary", null); await db.SaveChangesAsync(); }
        var a = await EvidenceAdditionTests.DraftAsync(f); var b = await EvidenceAdditionTests.DraftAsync(f);
        var input = (await Input(f, a.Id, b.Id)) with { Description = "  Synthetic text  " };
        using var submitted = await Submit(f, owner, input); Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var id = (await ApiFixture.JsonAsync(submitted)).GetValue<Guid>();
        using var reordered = await Submit(f, owner, input with { DocumentIds = [b.Id, a.Id], Description = "Synthetic text" }); Assert.Equal(HttpStatusCode.OK, reordered.StatusCode);
        foreach (var changed in new[] { input with { StartDate = input.StartDate.AddDays(1) }, input with { EndDate = input.EndDate.AddDays(1) },
            input with { ExpectedTypeVersion = Guid.NewGuid() }, input with { ExpectedPolicyVersion = Guid.NewGuid() }, input with { LeaveTypeId = Guid.NewGuid() },
            input with { Description = "Changed synthetic text" }, input with { DocumentIds = [a.Id] } })
        { using var conflict = await Submit(f, owner, changed); Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); }
        await AssertOne(f, id, 2);
        var separate = input with { SubmissionKey = Guid.NewGuid(), DocumentIds = [] };
        using var independent = await Submit(f, owner, separate); Assert.Equal(HttpStatusCode.OK, independent.StatusCode);
        await using var state = f.Context(); Assert.Equal(2, await state.LeaveRequests.CountAsync()); Assert.Equal(2, await state.LeaveSubmissionOutcomes.CountAsync());
    }

    /// <summary>Outcome inspection is read-only, never discloses another owner, and rechecks live roles/lifecycle/credentials.</summary>
    [Theory]
    [InlineData("role", 403)]
    [InlineData("inactive", 401)]
    [InlineData("pending", 401)]
    [InlineData("credential", 401)]
    public async Task CurrentAccessAndMissingOutcome(string change, int expected)
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee"); var input = await Input(f);
        using (var missing = await Check(f, owner, input)) { Assert.Equal(HttpStatusCode.OK, missing.StatusCode); Assert.Null((await ApiFixture.JsonAsync(missing))["requestId"]); }
        await using (var empty = f.Context()) { Assert.False(await empty.LeaveRequests.AnyAsync()); Assert.False(await empty.LeaveSubmissionOutcomes.AnyAsync()); }
        using (var created = await Submit(f, owner, input)) Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using (var other = await Check(f, await f.LoginAsync("unrelated"), input)) { Assert.Equal(HttpStatusCode.OK, other.StatusCode); Assert.Null((await ApiFixture.JsonAsync(other))["requestId"]); }
        using (var hr = await Check(f, await f.LoginAsync("hr"), input)) Assert.Equal(HttpStatusCode.Forbidden, hr.StatusCode);
        await using (var db = f.Context())
        {
            if (change == "role") db.UserRoles.RemoveRange(await db.UserRoles.Where(r => r.UserId == f.User("employee").Id).ToListAsync());
            if (change == "inactive") (await db.Employees.FindAsync(f.Employee("employee").Id))!.Deactivate(f.Employee("hr").Id, "Synthetic");
            if (change == "pending") (await db.Users.FindAsync(f.User("employee").Id))!.RequiresActivation = true;
            if (change == "credential") (await db.Users.FindAsync(f.User("employee").Id))!.CredentialVersion = Guid.NewGuid();
            await db.SaveChangesAsync();
        }
        using var denied = await Check(f, owner, input); Assert.Equal(expected, (int)denied.StatusCode);
        using var deniedSubmit = await Submit(f, owner, input); Assert.Equal(expected, (int)deniedSubmit.StatusCode);
        await using var persisted = f.Context(); Assert.Single(await persisted.LeaveRequests.ToArrayAsync()); Assert.Single(await persisted.LeaveSubmissionOutcomes.ToArrayAsync());
    }

    /// <summary>A late outcome-insert failure rolls back earlier SaveChanges, document binding, audit and notification outbox together.</summary>
    [Fact]
    public async Task LateFailureRollsBackEverything()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee"); var draft = await EvidenceAdditionTests.DraftAsync(f); var input = await Input(f, draft.Id);
        await using (var db = f.Context()) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_outcome BEFORE INSERT ON LeaveSubmissionOutcomes BEGIN SELECT RAISE(ABORT, 'Synthetic late failure'); END;");
        using (var failed = await Submit(f, owner, input)) Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        await using (var db = f.Context())
        {
            Assert.False(await db.LeaveRequests.AnyAsync()); Assert.False(await db.AuditEntries.AnyAsync()); Assert.False(await db.LeaveNotificationEvents.AnyAsync()); Assert.False(await db.LeaveSubmissionOutcomes.AnyAsync());
            Assert.Null((await db.SupportingDocuments.SingleAsync()).RequestId);
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_outcome");
        }
        using var retry = await Submit(f, owner, input); Assert.Equal(HttpStatusCode.OK, retry.StatusCode); await AssertOne(f, (await ApiFixture.JsonAsync(retry)).GetValue<Guid>(), 1);
    }

    /// <summary>Legacy callers intentionally remain unkeyed; no fabricated historical execution identities are added.</summary>
    [Fact]
    public async Task LegacyCallsRemainCompatibleAndModelMatches()
    {
        await using var f = await ApiFixture.StartAsync(); var id = await f.SubmitAsync("employee");
        var input = await Input(f);
        using var rejected = await f.SendAsync(await f.LoginAsync("employee"), "leave-requests", HttpMethod.Post, new {
            input.LeaveTypeId, StartDate = DateTime.UtcNow.Date.AddDays(-2), EndDate = DateTime.UtcNow.Date.AddDays(-1), input.ExpectedTypeVersion, input.ExpectedPolicyVersion });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode); Assert.Equal("A validation error occurred.", (await ApiFixture.JsonAsync(rejected))["title"]!.GetValue<string>());
        await using var db = f.Context(); Assert.False(db.Database.HasPendingModelChanges()); Assert.False(await db.LeaveSubmissionOutcomes.AnyAsync());
        Assert.Equal(id, (await db.LeaveRequests.SingleAsync()).Id); Assert.Single(await db.AuditEntries.ToArrayAsync());
    }

    /// <summary>A synthetic historically elapsed keyed request proves recovery bypasses the new-attempt date gate without changing the production clock.</summary>
    [Fact]
    public async Task ElapsedDatesRecoverButCannotCreateNewAttempt()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee");
        var input = (await Input(f)) with { StartDate = DateTime.UtcNow.Date.AddDays(-3), EndDate = DateTime.UtcNow.Date.AddDays(-2) };
        Guid id;
        await using (var db = f.Context())
        {
            var request = LeaveRequest.Create(f.Employee("employee").Id, input.LeaveTypeId, input.StartDate, input.EndDate);
            db.LeaveRequests.Add(request); id = request.Id;
            db.LeaveSubmissionOutcomes.Add(new LeaveSubmissionOutcome { OwnerId = request.EmployeeId, Key = input.SubmissionKey, RequestId = id,
                PayloadHash = HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest.LeaveSubmissionIdentity.Hash(new() {
                    LeaveTypeId = input.LeaveTypeId, StartDate = input.StartDate, EndDate = input.EndDate, ExpectedTypeVersion = input.ExpectedTypeVersion,
                    ExpectedPolicyVersion = input.ExpectedPolicyVersion, DocumentIds = [], Description = null }) });
            await db.SaveChangesAsync();
        }
        using var recovered = await Submit(f, owner, input); Assert.Equal(HttpStatusCode.OK, recovered.StatusCode); Assert.Equal(id, (await ApiFixture.JsonAsync(recovered)).GetValue<Guid>());
        using var fresh = await Submit(f, owner, input with { SubmissionKey = Guid.NewGuid() }); Assert.Equal(HttpStatusCode.BadRequest, fresh.StatusCode);
        Assert.Equal("A validation error occurred.", (await ApiFixture.JsonAsync(fresh))["title"]!.GetValue<string>());
        await using var state = f.Context(); Assert.Single(await state.LeaveRequests.ToArrayAsync()); Assert.Single(await state.LeaveSubmissionOutcomes.ToArrayAsync()); Assert.Single(await state.AuditEntries.ToArrayAsync());
    }

    /// <summary>Stopped-writer encrypted recovery retains associations and hashes, while security invalidation removes old authority.</summary>
    [Fact]
    public async Task EncryptedRecoveryRetainsOutcomeWithoutSessionAuthority()
    {
        await using var f = await ApiFixture.StartAsync(); var owner = await f.LoginAsync("employee"); var draft = await EvidenceAdditionTests.DraftAsync(f); var input = await Input(f, draft.Id);
        using var created = await Submit(f, owner, input); Assert.Equal(HttpStatusCode.OK, created.StatusCode); var id = (await ApiFixture.JsonAsync(created)).GetValue<Guid>();
        var authentication = f.AuthenticationEnvironment(); await f.StopAsync();
        await using var drill = await ActivationRecoveryFixture.CreateAsync(authentication);
        await drill.ToolAsync("keygen", "--output", drill.Key);
        await drill.ToolAsync("backup", "--database", f.DatabasePath, "--private-root", f.PrivateRoot, "--output", drill.Package, "--key-file", drill.Key);
        await drill.ToolAsync("restore", "--package", drill.Package, "--destination", drill.Destination, "--key-file", drill.Key, "--invalidate-security");
        await using (var db = ActivationRecoveryFixture.Context(drill.Database))
        {
            var outcome = await db.LeaveSubmissionOutcomes.SingleAsync(); Assert.Equal(id, outcome.RequestId); Assert.Equal(input.SubmissionKey, outcome.Key); Assert.Equal(f.Employee("employee").Id, outcome.OwnerId);
            Assert.Equal(id, (await db.SupportingDocuments.SingleAsync()).RequestId); Assert.Single(await db.AuditEntries.ToArrayAsync());
            Assert.All(await db.Users.ToArrayAsync(), u => Assert.Null(u.PasswordHash));
        }
        var host = await drill.StartHostAsync(drill.Database, Path.Combine(drill.Destination, "private"));
        using var denied = await host.SendAsync(owner, "leave-requests/submission-outcome", HttpMethod.Post, input); Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await host.StopAsync(); drill.AssertPermissions();
    }
}
