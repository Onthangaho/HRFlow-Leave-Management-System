using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json.Nodes;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Owns newly migrated synthetic SQLite and two real API processes; never opens an existing employee store.</summary>
public sealed class ApiFixture : IAsyncDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "hrflow-regression-" + Guid.NewGuid().ToString("N"));
    private readonly string pickupProfile = "Regression-" + Guid.NewGuid().ToString("N");
    private readonly List<Process> processes = [];
    private readonly Dictionary<string, (TaskCompletionSource Signal, int Remaining)> entries = [];
    private readonly object gate = new();
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string password = "Aa!9" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
    private readonly Dictionary<string, Employee> employees = [];
    private readonly Dictionary<string, ApplicationUser> users = [];
    private Guid department;
    private LeaveType type = null!;
    private Uri[] urls = [];

    /// <summary>Fresh private path shared only by this fixture's separate contexts/processes.</summary>
    public string DatabasePath => Path.Combine(root, "fixture.sqlite");
    /// <summary>Private runtime descriptor read only by the browser harness, never committed or reported.</summary>
    public object BrowserState => new { password, api = urls[0].ToString().TrimEnd('/'), employees = employees.ToDictionary(e => e.Key, e => new { id = e.Value.Id, email = e.Value.Email, name = e.Value.FullName }), typeId = type.Id };
    /// <summary>Locates build outputs without relying on machine-specific repository paths.</summary>
    public static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HRFlow.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Run fixtures from the repository build output.");
        }
    }
    /// <summary>Disables fixture parallelism at suite level; each case still owns entirely separate persistent storage.</summary>
    public static async Task<ApiFixture> StartAsync(string origin = "http://127.0.0.1:5173", CancellationToken cancellationToken = default)
    {
        var fixture = new ApiFixture();
        try { await fixture.InitializeAsync(origin, cancellationToken); return fixture; }
        catch { await fixture.DisposeAsync(); throw; }
    }
    /// <summary>Independent EF context for persisted-row assertions, not EF InMemory.</summary>
    public HRFlowDbContext Context() => new(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=" + DatabasePath + ";Foreign Keys=True;Default Timeout=3").Options);
    /// <summary>Independent raw SQLite connection for explicit barriers and rollback injection.</summary>
    public SqliteConnection Connection()
    {
        var connection = new SqliteConnection("Data Source=" + DatabasePath + ";Foreign Keys=True;Default Timeout=3");
        connection.Open(); return connection;
    }
    /// <summary>Fixture actor IDs are generated, not client-selected authorization targets.</summary>
    public Employee Employee(string actor) => employees[actor];
    /// <summary>Allows controlled persisted lifecycle/role changes through a separate connection.</summary>
    public ApplicationUser User(string actor) => users[actor];
    /// <summary>Uses real login and never prints credentials/tokens in failure messages.</summary>
    public async Task<string> LoginAsync(string actor)
    {
        using var response = await SendAsync(null, "auth/login", HttpMethod.Post, new { email = employees[actor].Email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await JsonAsync(response))["accessToken"]!.GetValue<string>();
    }
    /// <summary>Bounded HTTP calls use either independently running API process.</summary>
    public Task<HttpResponseMessage> SendAsync(string? token, string path, HttpMethod? method = null, object? body = null, int server = 0)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, new Uri(urls[server], "api/v1/" + path));
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }
    /// <summary>Reads only known synthetic responses; assertions avoid raw HTTP payload dumps.</summary>
    public static async Task<JsonNode> JsonAsync(HttpResponseMessage response) => JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    /// <summary>Submits through the live API with current selector versions and future inclusive dates.</summary>
    public async Task<Guid> SubmitAsync(string actor, int offset = 1, int days = 1)
    {
        var token = await LoginAsync(actor);
        using var selector = await SendAsync(token, "leave-types");
        var selected = (await JsonAsync(selector)).AsArray().Single(t => t!["id"]!.GetValue<Guid>() == type.Id)!;
        var date = DateTime.UtcNow.Date.AddDays(offset);
        using var response = await SendAsync(token, "leave-requests", HttpMethod.Post, new {
            leaveTypeId = type.Id, startDate = date, endDate = date.AddDays(days - 1),
            expectedTypeVersion = selected["version"]!.GetValue<Guid>(), expectedPolicyVersion = selected["policyVersion"]!.GetValue<Guid>() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await JsonAsync(response)).GetValue<Guid>();
    }
    /// <summary>Creates a legacy-style Pending fixture only for relationship denial tests, not statutory calculations.</summary>
    public async Task<Guid> PendingAsync(string owner)
    {
        await using var context = Context();
        var request = LeaveRequest.Create(employees[owner].Id, type.Id, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(1));
        context.LeaveRequests.Add(request); await context.SaveChangesAsync(); return request.Id;
    }
    /// <summary>Arms an explicit HTTP endpoint-entry barrier, observed from safe routing lifecycle logs before releasing the writer.</summary>
    public Task EnteredAsync(string action, int count = 1)
    {
        lock (gate) { var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); entries.Add(action, (signal, count)); return signal.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
    }
    /// <summary>Existing HR create contract keeps password establishment in activation.</summary>
    public object Creation(string email, string number) => new { fullName = "Synthetic import", email, employeeNumber = number,
        employmentStartDate = "2020-01-01", departmentId = department, roles = new[] { "Employee" } };

    private async Task InitializeAsync(string origin, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(root); RestrictDirectory(root); Directory.CreateDirectory(Path.Combine(root, "private"));
        await using (var context = Context())
        {
            await context.Database.MigrateAsync(cancellationToken);
            var engineering = new Department { Name = "Synthetic Engineering" }; var hr = new Department { Name = "Human Resources" };
            context.Departments.AddRange(engineering, hr); await context.SaveChangesAsync(cancellationToken); department = engineering.Id;
            var roles = new[] { "Employee", "Manager", "HR Administrator" }.ToDictionary(n => n, n => new IdentityRole<Guid>(n) { Id = Guid.NewGuid(), NormalizedName = n.ToUpperInvariant() });
            context.Roles.AddRange(roles.Values);
            var definitions = new[] {
                ("hr", "hr.administrator@hrflow.local", new[] {"HR Administrator"}),
                ("senior", "senior.manager@hrflow.local", new[] {"Manager"}),
                ("manager", "manager@hrflow.local", new[] {"Manager"}),
                ("employee", "employee@hrflow.local", new[] {"Employee"}),
                ("combined", "combined@synthetic.invalid", new[] {"Employee","Manager","HR Administrator"}),
                ("unrelated", "unrelated@synthetic.invalid", new[] {"Employee"}),
                ("cross", "cross@synthetic.invalid", new[] {"Employee"}) };
            var normalizer = new UpperInvariantLookupNormalizer();
            foreach (var (actor, email, capabilities) in definitions)
            {
                var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email,
                    NormalizedEmail = normalizer.NormalizeEmail(email), NormalizedUserName = normalizer.NormalizeName(email),
                    EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString() };
                user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, password);
                context.Users.Add(user); users.Add(actor, user);
                context.UserRoles.AddRange(capabilities.Select(r => new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roles[r].Id }));
                var employee = HRFlow.Domain.Entities.Employee.Create("Synthetic " + actor, email, actor is "hr" or "cross" ? hr.Id : department);
                employee.SetIdentityUser(user.Id.ToString()); employees.Add(actor, employee); context.Employees.Add(employee);
            }
            employees["manager"].AssignManager(employees["senior"].Id);
            employees["employee"].AssignManager(employees["manager"].Id);
            employees["combined"].AssignManager(employees["senior"].Id);
            employees["unrelated"].AssignManager(employees["senior"].Id);
            employees["cross"].AssignManager(employees["manager"].Id);
            var policy = LeavePolicy.Create("Synthetic policy", false, 20); type = LeaveType.Create("Synthetic leave", policy);
            context.LeavePolicies.Add(policy); context.LeaveTypes.Add(type); await context.SaveChangesAsync(cancellationToken);
        }
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(48)); var pepper = Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
        urls = new Uri[2];
        for (var index = 0; index < urls.Length; index++)
        {
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(RepositoryRoot, "src/HRFlow.Api"),
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add(Path.Combine(RepositoryRoot, "src/HRFlow.Api/bin/Debug/net8.0/HRFlow.Api.dll"));
            var essential = new[] { "PATH", "DOTNET_ROOT", "HOME", "USERPROFILE", "LOCALAPPDATA", "SystemRoot", "TEMP", "TMP" }.ToDictionary(k => k, Environment.GetEnvironmentVariable);
            start.Environment.Clear(); foreach (var (name, value) in essential) if (value is not null) start.Environment[name] = value;
            foreach (var (name, value) in new Dictionary<string, string> {
                ["ASPNETCORE_ENVIRONMENT"]="Development", ["ASPNETCORE_URLS"]="http://127.0.0.1:0", ["AllowedHosts"]="127.0.0.1",
                ["ConnectionStrings__DefaultConnection"]="Data Source="+DatabasePath+";Foreign Keys=True;Default Timeout=3",
                ["Storage__LocalFileSystem"]="true", ["Storage__PrivateFilesRoot"]=Path.Combine(root,"private"),
                ["Authentication__Jwt__Issuer"]="HRFlow.Regression", ["Authentication__Jwt__Audience"]="HRFlow.Regression.Client",
                ["Authentication__Jwt__SigningKey"]=key, ["Authentication__Jwt__RefreshTokenPepper"]=pepper,
                ["Activation__ApplicationOrigin"]=origin, ["Activation__Delivery"]="PrivatePickup", ["Activation__PickupProfile"]=pickupProfile,
                ["Deployment__CorsOrigins__0"]=origin, ["Logging__LogLevel__Default"]="Warning",
                ["Logging__LogLevel__Microsoft.AspNetCore.Routing.EndpointMiddleware"]="Information",
                ["Logging__LogLevel__Microsoft.Hosting.Lifetime"]="Information",
                ["Seeding__HrAdministratorPassword"]=password,["Seeding__EmployeePassword"]=password,["Seeding__ManagerPassword"]=password,["Seeding__SeniorManagerPassword"]=password }) start.Environment[name] = value;
            var process = new Process { StartInfo = start };
            var listening = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.OutputDataReceived += (_, line) => { Observe(line.Data); const string prefix = "Now listening on: "; var value = line.Data; if (value is not null && value.Contains(prefix)) listening.TrySetResult(new Uri(value[(value.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length)..].Trim())); }; process.ErrorDataReceived += (_, _) => { };
            process.Start(); processes.Add(process); process.BeginOutputReadLine(); process.BeginErrorReadLine();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); timeout.CancelAfter(TimeSpan.FromSeconds(30));
            urls[index] = await listening.Task.WaitAsync(timeout.Token);
            while (true)
            {
                if (process.HasExited) throw new InvalidOperationException("Disposable API exited before readiness; private configuration was not logged.");
                try { using var response = await client.GetAsync(new Uri(urls[index], "health/ready"), timeout.Token); if (response.IsSuccessStatusCode) break; }
                catch (HttpRequestException) { }
                await Task.Delay(25, timeout.Token); // Readiness polling, never concurrency ordering.
            }
        }
    }
    private void Observe(string? line)
    {
        if (line is null || !line.Contains("Executing endpoint")) return;
        lock (gate) foreach (var action in entries.Keys.ToArray()) if (line.Contains("." + action + " ", StringComparison.Ordinal)) { var entry = entries[action]; if (entry.Remaining == 1) { entry.Signal.TrySetResult(); entries.Remove(action); } else entries[action] = (entry.Signal, entry.Remaining - 1); }
    }
    internal static void RestrictDirectory(string path)
    {
        if (!OperatingSystem.IsWindows()) { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); return; }
        var security = new DirectorySecurity(); security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(path).SetAccessControl(security);
    }
    /// <summary>Private browser runtime state must not inherit broadly readable file permissions.</summary>
    public static void RestrictFile(string path)
    {
        if (!OperatingSystem.IsWindows()) { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); return; }
        var security = new FileSecurity(); security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
    }
    /// <summary>Stops owned process trees before deleting only this fixture's generated private directories.</summary>
    public async ValueTask DisposeAsync()
    {
        var processesStopped = true;
        foreach (var process in processes)
        {
            try
            {
                if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
            }
            catch { processesStopped = false; } // Attempt every owned process; retain stores if any writer could still be alive.
            finally { process.Dispose(); }
        }
        client.Dispose(); SqliteConnection.ClearAllPools();
        if (!processesStopped) throw new InvalidOperationException("Owned fixture process cleanup failed; private stores retained for safe operator cleanup.");
        var pickup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HRFlow", "ActivationPickup", pickupProfile);
        foreach (var owned in new[] { root, pickup })
        {
            var parent = owned == root ? Path.GetFullPath(Path.GetTempPath()) : Path.GetFullPath(Path.GetDirectoryName(pickup)!);
            var absolute = Path.GetFullPath(owned);
            if (!absolute.StartsWith(parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || Path.GetFileName(absolute) != (owned == root ? Path.GetFileName(root) : pickupProfile)) throw new InvalidOperationException("Unsafe cleanup path.");
            if (Directory.Exists(owned) && (File.GetAttributes(owned) & FileAttributes.ReparsePoint) == 0) Directory.Delete(owned, recursive: true);
        }
    }
}
