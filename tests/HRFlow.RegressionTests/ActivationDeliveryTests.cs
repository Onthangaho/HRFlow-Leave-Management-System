using System.Net;
using System.Text;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Application.Exceptions;
using HRFlow.Domain.Common;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces.Services.Employees;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Runs real coordinator/Identity/SQLite writes with a private real SMTP sink, never a production transport.</summary>
public sealed class ActivationDeliveryTests
{
    /// <summary>Real MIME and password establishment must preserve role authority and leave GET harmless.</summary>
    [Fact]
    public async Task ManualMimeGetAndSingleUseRedemption()
    {
        await using var h = await Harness.StartAsync();
        var created = await h.CreateAsync(); Assert.Equal("Accepted", created.InvitationDeliveryState);
        var invitation = PrivateActivationSmtp.Invitation(await h.Smtp.MessageAsync());
        await using (var db = h.Api.Context())
        {
            var account = await db.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID");
            Assert.True(account.ActivationTokenHash is not null && account.ActivationTokenHash != invitation);
            Assert.NotNull(account.ActivationDispatchStartedAtUtc); Assert.Null(account.PasswordHash);
        }
        using var get = await h.Api.SendAsync(null, "auth/activation"); Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        await using (var beforePost = h.Api.Context()) { var pending = await beforePost.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID"); Assert.True(pending.RequiresActivation); Assert.Null(pending.PasswordHash); Assert.NotNull(pending.ActivationTokenHash); }
        var password = "Aa!9" + Guid.NewGuid().ToString("N");
        using var redeemed = await h.Api.SendAsync(null, "auth/activation", HttpMethod.Post, new { token = invitation, password });
        Assert.Equal(HttpStatusCode.NoContent, redeemed.StatusCode);
        using var repeated = await h.Api.SendAsync(null, "auth/activation", HttpMethod.Post, new { token = invitation, password });
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        await using var final = h.Api.Context(); var user = await final.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID");
        Assert.False(user.RequiresActivation); Assert.Null(user.ActivationTokenHash); Assert.Null(user.ActivationRecipientNormalizedEmail);
        Assert.Equal(new[] { "Employee" }, await (from ur in final.UserRoles join r in final.Roles on ur.RoleId equals r.Id where ur.UserId == user.Id select r.Name).ToArrayAsync());
        Assert.True(!h.Logs.Any(s => s.Contains(invitation, StringComparison.Ordinal) || s.Contains(password, StringComparison.Ordinal)));
    }
    /// <summary>Transport failure never rolls back or recreates an account that already committed.</summary>
    [Theory]
    [InlineData("reject", "DeliveryFailed")]
    [InlineData("timeout", "DeliveryUnknown")]
    [InlineData("disconnect", "DeliveryUnknown")]
    public async Task CommittedAccountSurvivesTransportFailure(string mode, string expected)
    {
        await using var h = await Harness.StartAsync(); h.Smtp.Mode = mode;
        var created = await h.CreateAsync(); Assert.Equal(expected, created.InvitationDeliveryState);
        await h.Api.RestartAsync();
        await using var db = h.Api.Context(); Assert.Single(await db.Employees.Where(e => e.Id == created.EmployeeId).ToArrayAsync());
        var user = await db.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID");
        Assert.True(user.RequiresActivation); Assert.Null(user.PasswordHash); Assert.Equal(expected, user.InvitationDeliveryState);
        var count = h.Smtp.Submissions;
        var secret = mode == "reject" ? "not-a-token" : PrivateActivationSmtp.Invitation(await h.Smtp.MessageAsync());
        // A committed generation cannot be redispatched, including after a new service scope/restart.
        if (mode != "reject") Assert.Equal(expected, await h.CallAsync(sp => sp.GetRequiredService<IAccountActivationService>().DeliverAsync(h.Actor, user.Id, secret, default)));
        Assert.Equal(count, h.Smtp.Submissions); Assert.Equal(1,h.Smtp.Attempts);
    }
    /// <summary>A later employee or invitation state must win while another writer can proceed during blocked SMTP.</summary>
    [Theory]
    [InlineData("email", "DeliveryFailed")]
    [InlineData("inactive", "Unavailable")]
    [InlineData("expired", "Expired")]
    [InlineData("activated", "Activated")]
    [InlineData("profile", "Accepted")]
    public async Task LateAcknowledgementUsesCurrentGenerationAndEligibility(string change, string expected)
    {
        await using var h = await Harness.StartAsync(); h.Smtp.Mode = "hold";
        var creating = h.CreateAsync(); await h.Smtp.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await using (var db = h.Api.Context())
        {
            var account = await db.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID");
            var employee = await db.Employees.SingleAsync(e => e.IdentityUserId == account.Id.ToString());
            if (change == "email") { account.Email = "changed@synthetic.invalid"; account.NormalizedEmail = "CHANGED@SYNTHETIC.INVALID"; account.ActivationTokenHash = null; account.InvitationDeliveryState = "DeliveryFailed"; }
            if (change == "inactive") employee.Deactivate(h.Api.Employee("hr").Id, "Synthetic lifecycle check");
            if (change == "expired") account.ActivationExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            if (change == "activated") { account.RequiresActivation = false; account.ActivationTokenHash = null; account.InvitationDeliveryState = "Activated"; }
            if (change == "profile") employee.Update("Synthetic revised name", employee.Email, employee.DepartmentId);
            await db.SaveChangesAsync();
        }
        // This independent committed writer succeeds while SMTP is blocked, not merely after it returns.
        h.Smtp.Release.TrySetResult(); Assert.Equal(expected, (await creating).InvitationDeliveryState);
    }
    /// <summary>Revoked roles or bearer generations cannot authorise a late acknowledgement.</summary>
    [Theory]
    [InlineData("role")]
    [InlineData("credential")]
    public async Task RevocationDuringTransportCannotAcknowledge(string kind)
    {
        await using var h = await Harness.StartAsync(); h.Smtp.Mode = "hold";
        var task = h.CreateAsync(); await h.Smtp.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await using(var db = h.Api.Context())
        {
            if (kind == "role") db.UserRoles.RemoveRange(await db.UserRoles.Where(r => r.UserId == h.Actor).ToArrayAsync());
            else (await db.Users.FindAsync(h.Actor))!.CredentialVersion = Guid.NewGuid();
            await db.SaveChangesAsync();
        }
        h.Smtp.Release.TrySetResult();
        if (kind == "role") await Assert.ThrowsAsync<ForbiddenException>(() => task); else await Assert.ThrowsAsync<InvalidCredentialsException>(() => task);
        await using var final = h.Api.Context(); Assert.Equal("PendingDelivery", (await final.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID")).InvitationDeliveryState);
    }
    /// <summary>A late database failure leaves a durable dispatch claim and requires explicit rotated recovery.</summary>
    [Fact]
    public async Task FailedAcknowledgementReturnsPersistedStateWithoutReplay()
    {
        await using var h = await Harness.StartAsync();
        h.Smtp.AfterMessage = async () => { await using var db = h.Api.Context(); await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailInvitationAck BEFORE UPDATE OF InvitationDeliveryState ON AspNetUsers BEGIN SELECT RAISE(ABORT,'synthetic acknowledgement failure'); END"); };
        var result = await h.CreateAsync(); Assert.Equal("PendingDelivery", result.InvitationDeliveryState); Assert.Equal(1, h.Smtp.Submissions);
        await using(var db = h.Api.Context()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailInvitationAck");
        var invitation = PrivateActivationSmtp.Invitation(await h.Smtp.MessageAsync());
        await h.Api.RestartAsync(); Assert.Equal(1, h.Smtp.Submissions);
        Guid identity;await using(var db=h.Api.Context())identity=(await db.Users.SingleAsync(u=>u.NormalizedEmail=="INVITED@SYNTHETIC.INVALID")).Id;
        Assert.Equal("PendingDelivery",await h.CallAsync(sp=>sp.GetRequiredService<IAccountActivationService>().DeliverAsync(h.Actor,identity,invitation,default)));
        Assert.Equal(1,h.Smtp.Submissions);
        await using (var final = h.Api.Context())
        {
            var pending = await final.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID");
            Assert.NotNull(pending.ActivationDispatchStartedAtUtc);
            // The encrypted recovery test independently verifies this exact removal of authority.
            // Even possession of the old in-memory token must not dispatch after those fields clear.
            pending.ActivationTokenHash = null; pending.ActivationRecipientNormalizedEmail = null;
            pending.ActivationDispatchStartedAtUtc = null; pending.ActivationExpiresAtUtc = null;
            pending.InvitationDeliveryState = "Expired";
            await final.SaveChangesAsync();
        }
        Assert.Equal("Expired", await h.CallAsync(sp => sp.GetRequiredService<IAccountActivationService>().DeliverAsync(h.Actor, identity, invitation, default)));
        Assert.Equal(1, h.Smtp.Attempts);
    }
    /// <summary>Original resend versions prevent duplicate rotation and obsolete acknowledgements cannot replace current feedback.</summary>
    [Fact]
    public async Task ConcurrentResendRotatesAndOldAckCannotOverwriteNewOutcome()
    {
        await using var h = await Harness.StartAsync(); h.Smtp.Mode = "hold";
        var original = h.CreateAsync(); await h.Smtp.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var oldSecret = PrivateActivationSmtp.Invitation(await h.Smtp.MessageAsync());
        Guid employee; Guid version; await using(var db = h.Api.Context()) { var e = await db.Employees.SingleAsync(e => e.Email == "invited@synthetic.invalid"); employee = e.Id; version = e.Version; }
        h.Smtp.Mode = "reject";
        var resend = h.CallAsync(sp => sp.GetRequiredService<IAccountActivationService>().ResendAsync(h.Actor, employee, version, default));
        var duplicate = h.CallAsync(sp => sp.GetRequiredService<IAccountActivationService>().ResendAsync(h.Actor, employee, version, default));
        var results = await Task.WhenAll(Outcome(resend), Outcome(duplicate)); Assert.Contains("DeliveryFailed", results); Assert.Contains("conflict", results);
        h.Smtp.Release.TrySetResult(); Assert.Equal("DeliveryFailed", (await original).InvitationDeliveryState);
        using var redeemed = await h.Api.SendAsync(null, "auth/activation", HttpMethod.Post, new { token = oldSecret, password = "Aa!9" + Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.BadRequest, redeemed.StatusCode);
    }
    private static async Task<string> Outcome(Task<string> task) { try { return await task; } catch (WriteConflictException) { return "conflict"; } }
    /// <summary>Durable CSV outcomes and live delivery feedback cannot turn a committed employee back into a creatable row.</summary>
    [Fact]
    public async Task CsvFailureRefreshAndRepeatedConfirmationKeepCreatedRows()
    {
        await using var h = await Harness.StartAsync(); h.Smtp.Mode = "reject";
        var columns = new[] { "FirstName", "LastName", "EmailAddress", "EmployeeNumber", "Roles", "Department", "EmploymentStartDate" };
        var csv = Encoding.UTF8.GetBytes(string.Join(',', columns) + "\nSynthetic,CSV,csv-delivery@synthetic.invalid,CSV-DELIVERY,Employee,Synthetic Engineering,2020-01-01\n");
        var preview = await h.CallAsync(sp => sp.GetRequiredService<ICsvOnboardingService>().PreviewAsync(h.Actor, csv, columns.ToDictionary(c => c, c => c), default));
        var key = Guid.NewGuid().ToString();
        var result = await h.CallAsync(sp => sp.GetRequiredService<ICsvOnboardingService>().ConfirmAsync(h.Actor, preview.Id, preview.Version, key, [2], default));
        Assert.Equal("Created", result.Rows[0].State); Assert.Equal("DeliveryFailed", result.Rows[0].DeliveryState);
        var again = await h.CallAsync(sp => sp.GetRequiredService<ICsvOnboardingService>().ConfirmAsync(h.Actor, preview.Id, result.Version, key, [2], default));
        Assert.Equal("Created", again.Rows[0].State);
        await using var db = h.Api.Context(); Assert.Single(await db.Users.Where(u => u.NormalizedEmail == "CSV-DELIVERY@SYNTHETIC.INVALID").ToArrayAsync());
    }
    /// <summary>Even corrupted recipient data must not reach MIME headers or a transport.</summary>
    [Fact]
    public async Task HeaderInjectionIsRejectedBeforeSmtp()
    {
        await using var h = await Harness.StartAsync();
        var created = await h.CreateAsync(); var secret = PrivateActivationSmtp.Invitation(await h.Smtp.MessageAsync());
        Guid id;
        await using(var db = h.Api.Context()) { var u = await db.Users.SingleAsync(u => u.NormalizedEmail == "INVITED@SYNTHETIC.INVALID"); id=u.Id; u.Email="safe@synthetic.invalid\r\nBcc: unsafe@synthetic.invalid"; u.ActivationRecipientNormalizedEmail=u.Email.ToUpperInvariant(); u.ActivationDispatchStartedAtUtc=null; await db.SaveChangesAsync(); }
        var state = await h.CallAsync(sp=>sp.GetRequiredService<IAccountActivationService>().DeliverAsync(h.Actor,id,secret,default));
        Assert.Equal("DeliveryFailed",state); Assert.Equal(1,h.Smtp.Submissions);
    }

    /// <summary>Two actual API processes must not rotate the same loaded resend version twice.</summary>
    [Fact]
    public async Task CrossProcessResendHasOneCommittedGeneration()
    {
        await using var api = await ApiFixture.StartAsync(); var bearer = await api.LoginAsync("hr");
        using var created = await api.SendAsync(bearer,"employees",HttpMethod.Post,api.Creation("cross-resend@synthetic.invalid","CROSS-RESEND"));
        Assert.Equal(HttpStatusCode.Created,created.StatusCode); var result=await ApiFixture.JsonAsync(created);
        var id=result["employeeId"]!.GetValue<Guid>(); var version=result["version"]!.GetValue<Guid>();
        string? original; await using(var db=api.Context()) original=(await db.Users.SingleAsync(u=>u.NormalizedEmail=="CROSS-RESEND@SYNTHETIC.INVALID")).ActivationTokenHash;
        using var connection=api.Connection();using var writer=connection.BeginTransaction(deferred:false);
        var entered=api.EnteredAsync("Resend",2);
        var a=api.SendAsync(bearer,$"employees/{id}/activation/resend",HttpMethod.Post,new {expectedVersion=version},0);
        var b=api.SendAsync(bearer,$"employees/{id}/activation/resend",HttpMethod.Post,new {expectedVersion=version},1);
        await entered;writer.Commit();var responses=await Task.WhenAll(a,b);
        Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Conflict);
        await using var final=api.Context();var user=await final.Users.SingleAsync(u=>u.NormalizedEmail=="CROSS-RESEND@SYNTHETIC.INVALID");
        Assert.True(original!=user.ActivationTokenHash);Assert.NotNull(user.ActivationDispatchStartedAtUtc);Assert.Equal("PickupReady",user.InvitationDeliveryState);
        Assert.Single(await final.Employees.Where(e=>e.Id==id).ToArrayAsync());
        foreach(var response in responses)response.Dispose();
    }

    private sealed class Harness : IAsyncDisposable
    {
        public ApiFixture Api { get; }
        public PrivateActivationSmtp Smtp { get; }
        public Guid Actor => Api.User("hr").Id;
        public List<string> Logs { get; } = [];
        private readonly ServiceProvider provider;
        private Harness(ApiFixture api, PrivateActivationSmtp smtp)
        {
            Api=api; Smtp=smtp;
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:DefaultConnection"]="Data Source="+api.DatabasePath+";Foreign Keys=True;Default Timeout=3",["Activation:ApplicationOrigin"]="http://127.0.0.1:5173",["Activation:DeliveryTimeoutSeconds"]="5" }).Build();
            var services=new ServiceCollection(); services.AddLogging(b=>b.AddProvider(new PrivateLogs(Logs))); services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IInitiatingCredential>(new Proof(Actor,api.User("hr").CredentialVersion)); services.AddSingleton<IRequestCorrelationContext>(new Correlation());
            services.AddInfrastructure(config); services.AddScoped<CurrentAccountAuthorization>(); services.AddSingleton<IActivationDelivery>(Smtp);
            provider=services.BuildServiceProvider();
        }
        public static async Task<Harness> StartAsync()
        {
            var api = await ApiFixture.StartAsync();
            PrivateActivationSmtp? smtp = null;
            try { smtp = new(); return new(api, smtp); }
            catch { try { if (smtp is not null) await smtp.DisposeAsync(); } finally { await api.DisposeAsync(); } throw; }
        }
        public async Task<T> CallAsync<T>(Func<IServiceProvider,Task<T>> action) { await using var scope=provider.CreateAsyncScope(); return await action(scope.ServiceProvider); }
        public Task<HRFlow.Domain.Models.Employees.EmployeeManagementResult> CreateAsync()=>CallAsync(sp=>sp.GetRequiredService<IEmployeeManagementService>().CreateEmployeeAsync(Actor,"Synthetic invited","invited@synthetic.invalid",Api.Employee("employee").DepartmentId,[EmployeeRoles.Employee],null,"INVITED-1",new DateOnly(2020,1,1),default));
        public async ValueTask DisposeAsync()
        {
            Smtp.Release.TrySetResult();
            try { await Smtp.DisposeAsync(); }
            finally { try { await provider.DisposeAsync(); } finally { await Api.DisposeAsync(); } }
        }
    }
    private sealed class Proof(Guid id,Guid version):IInitiatingCredential { public string? IdentityUserId=>id.ToString();public string? Version=>version.ToString(); }
    private sealed class Correlation:IRequestCorrelationContext { public string? CorrelationId=>"synthetic-activation"; }
    private sealed class PrivateLogs(List<string> entries):ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)=>new Sink(entries);
        public void Dispose() { }
        private sealed class Sink(List<string> entries):ILogger { public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;public bool IsEnabled(LogLevel level)=>true;public void Log<TState>(LogLevel level,EventId id,TState state,Exception? error,Func<TState,Exception?,string> formatter){lock(entries)entries.Add(formatter(state,error));} }
    }
}
