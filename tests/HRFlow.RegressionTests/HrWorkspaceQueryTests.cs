using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveRequests.Queries.GetHrWorkspace;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services.Employees;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Measures the actual endpoint separately from instrumented handler SQL, using new migrated synthetic stores.</summary>
public sealed class HrWorkspaceQueryTests
{
    private static readonly DateOnly PeriodStart = new(2026, 1, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 12, 31);
    private const string Endpoint = "reports/hr-workspace?start=2026-01-01&end=2026-12-31";

    /// <summary>Identical cardinalities and six serial requests distinguish first endpoint invocation from warm local calls.</summary>
    [Theory]
    [InlineData(25)]
    [InlineData(1000)]
    public async Task MeasureWorkspace(int additional)
    {
        await using var api = await ApiFixture.StartAsync();
        await SeedAsync(api, additional);
        var bearer = await api.LoginAsync("hr");
        var elapsed = new List<double>(); var bytes = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var watch = Stopwatch.StartNew();
            using var response = await api.SendAsync(bearer, Endpoint);
            var body = await response.Content.ReadAsByteArrayAsync(); watch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bytes.Add(body.Length); elapsed.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 2));
        }
        var counter = new SqlCounter();
        await using var services = Services(api, counter);
        using var scope = services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetHrWorkspaceQueryHandler>();
        counter.Reset();
        var result = await handler.Handle(new(api.User("hr").Id, PeriodStart, PeriodEnd, null), default);
        Assert.Equal(additional + 7, result.ActiveEmployees + result.InactiveEmployees);
        var commands = counter.Count;
        Assert.Equal(12, commands);
        await using var expectedContext = api.Context();
        var expected = await ReferenceAsync(expectedContext, result.AsOfUtc);
        AssertWorkforce(expected, result);
        Console.WriteLine(JsonSerializer.Serialize(new { workspaceEmployees = additional + 7,
            schedules = await CountSchedulesAsync(api), handlerSqlCommands = commands,
            responseBytes = bytes, firstEndpointCallMs = elapsed[0], warmEndpointCallsMs = elapsed.Skip(1).ToArray() }));
    }

    /// <summary>Legacy parsing, expiry boundaries and SAST dates match persisted scalar rules without mutating any records.</summary>
    [Fact]
    public async Task AggregateMatchesFormerRulesAndPreservesRows()
    {
        await using var api = await ApiFixture.StartAsync(); await SeedAsync(api, 24);
        var asOf = new DateTimeOffset(2026, 10, 21, 22, 0, 0, TimeSpan.Zero);
        await using (var db = api.Context())
        {
            var employees = await db.Employees.Where(e => e.FullName.StartsWith("Directory person ")).OrderBy(e => e.FullName).ToArrayAsync();
            var users = await db.Users.ToDictionaryAsync(u => u.Id);
            var first = users[Guid.Parse(employees[1].IdentityUserId!)];
            db.Entry(employees[1]).Property(e => e.IdentityUserId).CurrentValue = first.Id.ToString().ToUpperInvariant();
            first.ActivationExpiresAtUtc = null;
            var alternate = users[Guid.Parse(employees[2].IdentityUserId!)];
            db.Entry(employees[2]).Property(e => e.IdentityUserId).CurrentValue = alternate.Id.ToString("N");
            alternate.ActivationExpiresAtUtc = asOf.UtcDateTime.AddTicks(1);
            var alias = Employee.Create("Synthetic legacy alias", "alias@synthetic.invalid", employees[2].DepartmentId);
            alias.SetIdentityUser(alternate.Id.ToString()); db.Employees.Add(alias);
            users[Guid.Parse(employees[3].IdentityUserId!)].ActivationExpiresAtUtc = asOf.UtcDateTime;
            users[Guid.Parse(employees[11].IdentityUserId!)].ActivationExpiresAtUtc = asOf.UtcDateTime.AddTicks(1);
            users[Guid.Parse(employees[17].IdentityUserId!)].ActivationExpiresAtUtc = null;
            users[Guid.Parse(employees[18].IdentityUserId!)].ActivationExpiresAtUtc = asOf.UtcDateTime.AddTicks(-1);
            users[Guid.Parse(employees[19].IdentityUserId!)].ActivationExpiresAtUtc = null;
            db.Entry(employees[8]).Property(e => e.IdentityUserId).CurrentValue = "not-an-identity";
            db.Entry(employees[9]).Property(e => e.IdentityUserId).CurrentValue = null;
            db.Entry(employees[10]).Property(e => e.IdentityUserId).CurrentValue = Guid.NewGuid().ToString();
            db.WeeklyScheduleRevisions.RemoveRange(await db.WeeklyScheduleRevisions.Where(s => s.EmployeeId == employees[0].Id).ToArrayAsync());
            db.WeeklyScheduleRevisions.Add(WeeklyScheduleRevision.Create(employees[0].Id, api.Employee("hr").Id, "SAST boundary", new(2026, 10, 22), [480, 0, 0, 0, 0, 0, 0]));
            employees[4].Update(employees[4].FullName, employees[4].Email, api.Employee("hr").DepartmentId);
            db.Departments.Add(new Department { Name = "Synthetic zero department" });
            await db.SaveChangesAsync();
        }
        await using var beforeContext = api.Context(); var before = await FingerprintAsync(beforeContext);
        var frozen = new FrozenTime(asOf); var counter = new SqlCounter(); await using var services = Services(api, counter, frozen);
        using var scope = services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<GetHrWorkspaceQueryHandler>()
            .Handle(new(api.User("hr").Id, PeriodStart, PeriodEnd, null), default);
        Assert.Equal(12, counter.Count); Assert.Equal(1, frozen.Reads); Assert.Equal(asOf, result.AsOfUtc);
        Assert.Equal(new DateOnly(2026, 10, 22), result.BusinessToday);
        await using var afterContext = api.Context(); AssertWorkforce(await ReferenceAsync(afterContext, asOf), result);
        Assert.True(before == await FingerprintAsync(afterContext), "Workforce reads must preserve persisted records.");
        var empty = Assert.Single(result.OrganisationDepartments, d => d.Name == "Synthetic zero department");
        Assert.Equal(0, empty.ActiveEmployees + empty.InactiveEmployees);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<HRFlowDbContext>().ChangeTracker.Entries());
    }

    /// <summary>A genuinely empty migrated store produces zero aggregates rather than dropping departments or inventing accounts.</summary>
    [Fact]
    public async Task EmptyMigratedStoreHasZeroWorkforce()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrflow-workforce-" + Guid.NewGuid().ToString("N"));
        ApiFixture.CreatePrivateDirectory(root); var file = Path.Combine(root, "empty.sqlite"); ApiFixture.CreatePrivateFile(file);
        try
        {
            await using var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=" + file + ";Pooling=False").Options);
            await db.Database.MigrateAsync();
            await db.Database.OpenConnectionAsync();
            using var snapshot = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var transaction = await db.Database.UseTransactionAsync(snapshot);
            var result = await new HrWorkforceAttentionReader(db).ReadAsync(DateTimeOffset.UtcNow, default);
            Assert.Equal(new HrWorkforceAttention(0, 0, 0, 0, 0, 0, 0, 0, result.Departments), result);
            Assert.Empty(result.Departments); Assert.Empty(await db.Users.ToArrayAsync());
        }
        finally { SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }

    /// <summary>A second writer can reserve/write during a deferred snapshot; DELETE delays its commit, while WAL can commit immediately.</summary>
    [Theory]
    [InlineData("delete")]
    [InlineData("wal")]
    public async Task SnapshotAllowsWriterAndRejectsSubsequentRevokedCredential(string journalMode)
    {
        await using var api = await ApiFixture.StartAsync();
        using (var connection = api.Connection()) { using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=" + journalMode; Assert.Equal(journalMode, command.ExecuteScalar()); }
        var barrier = new ReadBarrier(); await using var services = Services(api, new(), barrier: barrier);
        using var scope = services.CreateScope();
        var reading = scope.ServiceProvider.GetRequiredService<GetHrWorkspaceQueryHandler>().Handle(new(api.User("hr").Id, PeriodStart, PeriodEnd, null), default);
        HrWorkspaceDto result;
        try
        {
            await barrier.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await using var writer = api.Context(); await using var transaction = await writer.Database.BeginTransactionAsync();
            await writer.Employees.Where(e => e.Id == api.Employee("employee").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.DepartmentId, api.Employee("hr").DepartmentId).SetProperty(e => e.IsActive, false));
            var role = await writer.Roles.Where(r => r.Name == "HR Administrator").Select(r => r.Id).SingleAsync();
            await writer.UserRoles.Where(r => r.UserId == api.User("hr").Id && r.RoleId == role).ExecuteDeleteAsync();
            var revokedVersion = Guid.NewGuid();
            await writer.Users.Where(u => u.Id == api.User("hr").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CredentialVersion, revokedVersion));
            if (journalMode == "wal") await transaction.CommitAsync();
            barrier.Release.TrySetResult();
            result = await reading;
            if (journalMode == "delete") await transaction.CommitAsync();
        }
        // Observe the read even on writer failure before disposing its context or deleting the store.
        finally { barrier.Release.TrySetResult(); await reading; }
        Assert.Equal(7, result.ActiveEmployees); Assert.Equal(0, result.InactiveEmployees);
        Assert.Equal(5, result.OrganisationDepartments.Single(d => d.Id == api.Employee("employee").DepartmentId).ActiveEmployees);
        using var next = services.CreateScope();
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => next.ServiceProvider.GetRequiredService<GetHrWorkspaceQueryHandler>()
            .Handle(new(api.User("hr").Id, PeriodStart, PeriodEnd, null), default));
        await using var after = api.Context(); Assert.False((await after.Employees.FindAsync(api.Employee("employee").Id))!.IsActive);
    }

    /// <summary>The actual endpoint denies current-role/lifecycle failures and preserves additive personal capabilities.</summary>
    [Fact]
    public async Task HttpAuthorizationAndReportContractStayUnchanged()
    {
        await using var api = await ApiFixture.StartAsync(); var hr = await api.LoginAsync("hr"); var combined = await api.LoginAsync("combined");
        foreach (var actor in new[] { "employee", "manager" })
            Assert.Equal(HttpStatusCode.Forbidden, (await api.SendAsync(await api.LoginAsync(actor), Endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.SendAsync(combined, Endpoint)).StatusCode);
        var request = await api.SubmitAsync("employee", 10, 3);
        Assert.Equal(HttpStatusCode.NoContent, (await api.SendAsync(await api.LoginAsync("manager"), $"leave-requests/{request}/approve", HttpMethod.Post, new { })).StatusCode);
        await using var db = api.Context();
        await db.Employees.Where(e => e.Id == api.Employee("employee").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false).SetProperty(e => e.DepartmentId, api.Employee("hr").DepartmentId));
        var before = await FingerprintAsync(db);
        var period = $"start={DateTime.UtcNow:yyyy-MM-dd}&end={DateTime.UtcNow.AddDays(30):yyyy-MM-dd}";
        using var response = await api.SendAsync(hr, "reports/hr-workspace?" + period); Assert.Equal(HttpStatusCode.OK, response.StatusCode); var workspace = await ApiFixture.JsonAsync(response);
        using var report = await api.SendAsync(hr, "reports/department-leave?" + period);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True(JsonNode.DeepEquals(workspace["report"], await ApiFixture.JsonAsync(report)), "Workspace report must match the authoritative report endpoint.");
        Assert.Equal(3, workspace["report"]!["totals"]!["approvedRequestDays"]!.GetValue<int>());
        Assert.Equal(3, workspace["report"]!["totals"]!["inactiveApprovedRequestDays"]!.GetValue<int>());
        Assert.Equal(3, workspace["trends"]!.AsArray().Sum(t => t!["approvedRequestDays"]!.GetValue<int>()));
        Assert.True(before == await FingerprintAsync(db), "Reporting must not alter decisions, audits or workforce rows.");
        var role = await db.Roles.Where(r => r.Name == "HR Administrator").Select(r => r.Id).SingleAsync();
        await db.UserRoles.Where(r => r.UserId == api.User("combined").Id && r.RoleId == role).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await api.SendAsync(combined, Endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.SendAsync(combined, "leave-requests/dashboard")).StatusCode);
        await db.Users.Where(u => u.Id == api.User("hr").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.RequiresActivation, true));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.SendAsync(hr, Endpoint)).StatusCode);
        await db.Users.Where(u => u.Id == api.User("hr").Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.RequiresActivation, false));
        await db.Employees.Where(e => e.Id == api.Employee("hr").Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.SendAsync(hr, Endpoint)).StatusCode);
    }

    private static async Task<HrWorkforceAttention> ReferenceAsync(HRFlowDbContext db, DateTimeOffset asOf)
    {
        var employees = await db.Employees.AsNoTracking().ToArrayAsync(); var users = await db.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);
        var schedules = (await db.WeeklyScheduleRevisions.AsNoTracking().ToArrayAsync()).ToLookup(s => s.EmployeeId, s => s.EffectiveFrom);
        var today = HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard.GetPersonalDashboardQueryHandler.BusinessDate(asOf);
        var flags = employees.Select(e => EmploymentAttention.For(e, today, schedules[e.Id])).ToArray();
        var pending = 0; var failed = 0; var prepared = 0;
        foreach (var employee in employees.Where(e => e.IsActive))
        {
            if (!Guid.TryParse(employee.IdentityUserId, out var id) || !users.TryGetValue(id, out var account) || !account.RequiresActivation) continue;
            pending++;
            var state = !employees.Any(e => e.IdentityUserId == account.Id.ToString() && e.IsActive) ? "Unavailable"
                : account.ActivationExpiresAtUtc <= asOf.UtcDateTime ? "Expired" : account.InvitationDeliveryState;
            if (state is "DeliveryFailed" or "DeliveryUnknown") failed++;
            if (state == "PendingDelivery") prepared++;
        }
        var departments = await db.Departments.AsNoTracking().OrderBy(d => d.Name).ThenBy(d => d.Id).ToArrayAsync();
        return new(employees.Count(e => e.IsActive), employees.Count(e => !e.IsActive), flags.Count(f => f.NeedsEmploymentFacts),
            flags.Count(f => f.NeedsCurrentSchedule), flags.Count(f => f.HasFutureScheduleOnly), pending, failed, prepared,
            departments.Select(d => new HrDepartmentDto(d.Id, d.Name, employees.Count(e => e.DepartmentId == d.Id && e.IsActive), employees.Count(e => e.DepartmentId == d.Id && !e.IsActive))).ToArray());
    }
    private static void AssertWorkforce(HrWorkforceAttention expected, HrWorkspaceDto actual)
    {
        var received = new HrWorkforceAttention(actual.ActiveEmployees, actual.InactiveEmployees, actual.Attention.UnknownEmploymentFacts,
            actual.Attention.MissingCurrentSchedules, actual.Attention.FutureScheduleOnly, actual.Attention.PendingActivation,
            actual.Attention.DeliveryFailed, actual.Attention.DeliveryPending, actual.OrganisationDepartments);
        Assert.True(JsonSerializer.Serialize(expected) == JsonSerializer.Serialize(received), "Aggregates must match the independent former scalar algorithm.");
    }
    private static async Task<string> FingerprintAsync(HRFlowDbContext db) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
        Employees = await db.Employees.AsNoTracking().OrderBy(e => e.Id).ToArrayAsync(), Users = await db.Users.AsNoTracking().OrderBy(u => u.Id).ToArrayAsync(),
        Schedules = await db.WeeklyScheduleRevisions.AsNoTracking().OrderBy(s => s.Id).ToArrayAsync(),
        Roles = await db.UserRoles.AsNoTracking().OrderBy(r => r.UserId).ThenBy(r => r.RoleId).ToArrayAsync(),
        Requests = await db.LeaveRequests.AsNoTracking().OrderBy(r => r.Id).ToArrayAsync(), Audits = await db.AuditEntries.AsNoTracking().OrderBy(a => a.Id).ToArrayAsync()
    })));

    private static async Task<int> CountSchedulesAsync(ApiFixture api)
    {
        await using var db = api.Context(); return await db.WeeklyScheduleRevisions.CountAsync();
    }

    private static async Task SeedAsync(ApiFixture api, int count)
    {
        await DirectoryFixtureData.SeedAsync(api, count);
        await using var db = api.Context();
        var employees = await db.Employees.Where(e => e.EmployeeNumber != null && e.EmployeeNumber.StartsWith("DIR-"))
            .OrderBy(e => e.EmployeeNumber).ToArrayAsync();
        var users = await db.Users.ToDictionaryAsync(u => u.Id);
        for (var i = 0; i < employees.Length; i++)
        {
            var employee = employees[i]; var user = users[Guid.Parse(employee.IdentityUserId!)];
            var kind = i % 8;
            user.RequiresActivation = kind != 0;
            user.InvitationDeliveryState = kind switch { 1 or 6 or 7 => "DeliveryFailed", 2 => "DeliveryUnknown", 3 => "PendingDelivery", 4 => "Accepted", 5 => "PickupReady", _ => "Activated" };
            user.ActivationExpiresAtUtc = kind == 6 ? new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc) : new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (kind == 4) employee.ConfirmEmployment(employee.EmployeeNumber!, new DateOnly(2090, 1, 1));
            if (kind == 5) db.Entry(employee).Property(e => e.EmploymentStartDate).CurrentValue = null;
            if (kind == 6) db.Entry(employee).Property(e => e.EmployeeNumber).CurrentValue = null;
            if (kind == 7) employee.Deactivate(api.Employee("hr").Id, "Synthetic measurement lifecycle");
            if (kind is 0 or 3) db.WeeklyScheduleRevisions.Add(WeeklyScheduleRevision.Create(employee.Id, api.Employee("hr").Id, "Current synthetic", new(2020, 1, 1), [480, 0, 0, 0, 0, 0, 0]));
            if (kind is 0 or 2) db.WeeklyScheduleRevisions.Add(WeeklyScheduleRevision.Create(employee.Id, api.Employee("hr").Id, "Future synthetic", new(2090, 1, 1), [480, 0, 0, 0, 0, 0, 0]));
        }
        await db.SaveChangesAsync();
    }

    private static ServiceProvider Services(ApiFixture api, SqlCounter counter, TimeProvider? clock = null, ReadBarrier? barrier = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Data Source=" + api.DatabasePath + ";Foreign Keys=True;Default Timeout=3" }).Build();
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(config); services.AddSingleton<IHostEnvironment>(new PrivateEnvironment());
        services.AddSingleton(clock ?? TimeProvider.System);
        services.AddSingleton<IInitiatingCredential>(new Proof(api.User("hr").Id, api.User("hr").CredentialVersion));
        services.AddInfrastructure(config);
        services.AddScoped<HRFlowDbContext>(_ => new(new DbContextOptionsBuilder<HRFlowDbContext>()
            .UseSqlite(config.GetConnectionString("DefaultConnection")!).AddInterceptors(counter).Options));
        services.AddScoped<CurrentAccountAuthorization>(); services.AddScoped<GetHrWorkspaceQueryHandler>();
        if (barrier is not null) services.AddScoped<IHrWorkforceAttentionReader>(sp => new HeldReader(new HrWorkforceAttentionReader(sp.GetRequiredService<HRFlowDbContext>()), sp.GetRequiredService<HRFlowDbContext>(), barrier));
        return services.BuildServiceProvider();
    }

    private sealed class SqlCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public void Reset() => Count = 0;
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Count++; return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
    }
    private sealed class Proof(Guid id, Guid version) : IInitiatingCredential
    { public string? IdentityUserId => id.ToString(); public string? Version => version.ToString(); }
    private sealed class FrozenTime(DateTimeOffset now) : TimeProvider
    { public int Reads { get; private set; } public override DateTimeOffset GetUtcNow() { Reads++; return now; } }
    private sealed class ReadBarrier
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class HeldReader(IHrWorkforceAttentionReader inner, HRFlowDbContext db, ReadBarrier barrier) : IHrWorkforceAttentionReader
    {
        public async Task<HrWorkforceAttention> ReadAsync(DateTimeOffset asOf, CancellationToken token)
        {
            Assert.NotNull(db.Database.CurrentTransaction); barrier.Entered.TrySetResult();
            await barrier.Release.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
            return await inner.ReadAsync(asOf, token);
        }
    }
    private sealed class PrivateEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "HRFlow.RegressionTests";
        public string ContentRootPath { get; set; } = ApiFixture.RepositoryRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
