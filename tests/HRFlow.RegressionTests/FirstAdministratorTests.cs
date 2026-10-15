using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services.Operations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Real migrated private stores and operator processes exercise bootstrap without a provisioning HTTP bypass.</summary>
public sealed class FirstAdministratorTests
{
    /// <summary>Only the explicitly selected department is used; new and reused department paths preserve exact HR capability.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatesExactlyOneHrAndRefusesRepetition(bool reuse)
    {
        await using var store = await Store.CreateAsync();
        var options = store.Options;
        await using (var db = store.Context())
        {
            if (reuse)
            {
                var department = new Department { Id = Guid.NewGuid(), Name = "Explicit existing" };
                db.Departments.Add(department);
                db.Roles.Add(new IdentityRole<Guid>(EmployeeRoles.Employee) { Id = Guid.NewGuid(), NormalizedName = "EMPLOYEE" });
                await db.SaveChangesAsync(); options = options with { DepartmentId = department.Id, CreateDepartment = null };
            }
        }
        Assert.Equal(0, await store.CommandAsync(options));
        await using (var db = store.Context())
        {
            var user = await db.Users.SingleAsync(); var employee = await db.Employees.SingleAsync();
            Assert.True(employee.IsActive); Assert.False(user.RequiresActivation); Assert.NotNull(user.ActivatedAtUtc);
            Assert.Null(user.ActivationTokenHash); Assert.Null(user.ActivationExpiresAtUtc); Assert.Null(employee.ManagerId);
            Assert.Empty(await db.WeeklyScheduleRevisions.ToArrayAsync()); Assert.Empty(await db.AuditEntries.ToArrayAsync());
            Assert.Equal("ADMIN_001", employee.EmployeeNumber); Assert.Equal(new DateOnly(2024, 2, 29), employee.EmploymentStartDate);
            Assert.Equal(user.Id.ToString(), employee.IdentityUserId); Assert.Equal(3, await db.Roles.CountAsync());
            var membership = await db.UserRoles.SingleAsync();
            Assert.Equal(EmployeeRoles.HrAdministrator, (await db.Roles.SingleAsync(r => r.Id == membership.RoleId)).Name);
            Assert.Equal(user.Id, membership.UserId); Assert.Single(await db.Departments.ToArrayAsync());
        }
        Assert.NotEqual(0, await store.CommandAsync(options));
        await using var unchanged = store.Context();
        Assert.Equal(1, await unchanged.Users.CountAsync()); Assert.Equal(1, await unchanged.Employees.CountAsync());
        var identity = await unchanged.Users.SingleAsync(); var originalHash = identity.PasswordHash;
        await Assert.ThrowsAsync<InvalidDataException>(() => RecoveryPasswordService.RecoverAsync(store.Database,
            identity.Id, store.PasswordFile, ApiFixture.RepositoryRoot, true));
        await unchanged.Entry(identity).ReloadAsync();
        Assert.True(string.Equals(originalHash, identity.PasswordHash, StringComparison.Ordinal), "Recovery refusal must preserve the existing credential.");
    }

    /// <summary>Invalid Identity/domain inputs leave no roles, department or partial account behind.</summary>
    [Theory]
    [InlineData("password")]
    [InlineData("email")]
    [InlineData("name")]
    [InlineData("number")]
    [InlineData("number-length")]
    [InlineData("date")]
    [InlineData("date-range")]
    [InlineData("department-missing")]
    [InlineData("department-both")]
    public async Task InvalidInputsRollBack(string input)
    {
        await using var store = await Store.CreateAsync(); var options = store.Options;
        switch (input)
        {
            case "password": await File.WriteAllTextAsync(store.PasswordFile, "weak"); break;
            case "email": options = options with { Email = "not-an-email" }; break;
            case "name": options = options with { Name = " " }; break;
            case "number": options = options with { EmployeeNumber = "BAD NUMBER" }; break;
            case "number-length": options = options with { EmployeeNumber = new string('A', 33) }; break;
            case "date": options = options with { EmploymentStartDate = "2023-02-29" }; break;
            case "date-range": options = options with { EmploymentStartDate = "1899-12-31" }; break;
            case "department-missing": options = options with { CreateDepartment = null }; break;
            case "department-both": options = options with { DepartmentId = Guid.NewGuid() }; break;
        }
        await Assert.ThrowsAsync<FirstAdministratorException>(() => FirstAdministratorService.ProvisionAsync(options));
        await store.AssertEmptyAsync();
    }

    /// <summary>All pre-existing identities/profiles count, irrespective of activation, lifecycle or linkage.</summary>
    [Theory]
    [InlineData("identity")]
    [InlineData("pending")]
    [InlineData("profile")]
    [InlineData("inactive")]
    public async Task EveryNonemptyStoreIsRefused(string kind)
    {
        await using var store = await Store.CreateAsync();
        await using (var db = store.Context())
        {
            if (kind is "identity" or "pending") db.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), RequiresActivation = kind == "pending" });
            else
            {
                var department = new Department { Id = Guid.NewGuid(), Name = "Preserved" }; db.Departments.Add(department);
                var employee = Employee.Create("Preserved", "preserved@synthetic.invalid", department.Id);
                if (kind == "inactive") employee.Deactivate(employee.Id, "Synthetic fixture");
                db.Employees.Add(employee);
            }
            await db.SaveChangesAsync();
        }
        var refusal = await Assert.ThrowsAsync<FirstAdministratorException>(() => FirstAdministratorService.ProvisionAsync(store.Options));
        Assert.Equal(FirstAdministratorRefusal.NonemptyStore, refusal.Reason);
        await using var state = store.Context(); Assert.Empty(await state.Roles.ToArrayAsync());
        Assert.Equal(kind is "identity" or "pending" ? 1 : 0, await state.Users.CountAsync());
        Assert.Equal(kind is "profile" or "inactive" ? 1 : 0, await state.Employees.CountAsync());
    }

    /// <summary>Canonical role inconsistencies and department name collisions are never silently repaired.</summary>
    [Theory]
    [InlineData("role-case")]
    [InlineData("role-normalization")]
    [InlineData("role-duplicate")]
    [InlineData("department-duplicate")]
    [InlineData("department-unknown")]
    public async Task ConflictingConfigurationIsPreserved(string kind)
    {
        await using var store = await Store.CreateAsync(); var options = store.Options;
        await using (var db = store.Context())
        {
            if (kind.StartsWith("role"))
            {
                db.Roles.Add(new IdentityRole<Guid>(kind == "role-case" ? "employee" : "Employee")
                    { Id = Guid.NewGuid(), NormalizedName = kind == "role-normalization" ? "WRONG" : "EMPLOYEE" });
                if (kind == "role-duplicate") db.Roles.Add(new IdentityRole<Guid>("Employee") { Id = Guid.NewGuid(), NormalizedName = "OTHER" });
            }
            else if (kind == "department-duplicate")
                db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "  initial department  " });
            else options = options with { DepartmentId = Guid.NewGuid(), CreateDepartment = null };
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<FirstAdministratorException>(() => FirstAdministratorService.ProvisionAsync(options));
        await using var state = store.Context(); Assert.Empty(await state.Users.ToArrayAsync()); Assert.Empty(await state.Employees.ToArrayAsync());
        Assert.Equal(kind == "department-duplicate" ? 1 : 0, await state.Departments.CountAsync());
        Assert.Equal(kind == "role-duplicate" ? 2 : kind.StartsWith("role") ? 1 : 0, await state.Roles.CountAsync());
    }

    /// <summary>An actual SQLite late failure rolls back Identity's earlier saves and newly created role/department rows.</summary>
    [Fact]
    public async Task LateProfileFailureRollsBackEverything()
    {
        await using var store = await Store.CreateAsync();
        await using (var db = store.Context()) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailFirstAdmin BEFORE INSERT ON Employees BEGIN SELECT RAISE(ABORT,'synthetic late failure'); END");
        Assert.NotEqual(0, await store.CommandAsync(store.Options)); await store.AssertEmptyAsync();
    }

    /// <summary>Path, schema and credential-file failures are exercised through the real CLI without printing private input.</summary>
    [Theory]
    [InlineData("missing-db")]
    [InlineData("wrong-schema")]
    [InlineData("unsafe-db")]
    [InlineData("insecure-password")]
    public async Task UnsafeInputsAreRefused(string kind)
    {
        await using var store = await Store.CreateAsync(kind != "wrong-schema"); var options = store.Options;
        switch (kind)
        {
            case "missing-db": options = options with { Database = Path.Combine(store.Root, "missing.sqlite") }; break;
            case "unsafe-db": options = options with { Database = "relative.sqlite" }; break;
            case "insecure-password": store.MakePasswordInsecure(); break;
        }
        Assert.NotEqual(0, await store.CommandAsync(options));
        if (kind != "wrong-schema") await store.AssertEmptyAsync();
        Assert.False(File.Exists(Path.Combine(store.Root, "missing.sqlite")));
    }

    /// <summary>Two independent operator processes are released at an explicit stdin barrier; at most one commits.</summary>
    [Fact]
    public async Task SeparateProcessesCannotBootstrapTwice()
    {
        await using var store = await Store.CreateAsync();
        var descriptor = Path.Combine(store.Root, "options.json"); await File.WriteAllTextAsync(descriptor, JsonSerializer.Serialize(store.Options)); ApiFixture.RestrictFile(descriptor);
        using var a = store.StartRace(descriptor); using var b = store.StartRace(descriptor);
        try
        {
            Assert.Equal("Ready", await a.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)));
            Assert.Equal("Ready", await b.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)));
            await a.StandardInput.WriteLineAsync(); await b.StandardInput.WriteLineAsync();
            await Task.WhenAll(a.WaitForExitAsync(), b.WaitForExitAsync()).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Single(new[] { a.ExitCode, b.ExitCode }, code => code == 0);
            await using var db = store.Context(); Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(1, await db.Employees.CountAsync());
            Assert.Equal(1, await db.UserRoles.CountAsync()); Assert.Equal(1, await db.Departments.CountAsync());
        }
        finally { await Store.StopAsync(a); await Store.StopAsync(b); }
    }

    /// <summary>Provisioned credentials work under real Production HTTPS without seeding or invitation pickup.</summary>
    [Fact]
    public async Task ProductionLoginGrantsHrButNeverPersonalOrDecisionAccess()
    {
        await using var store = await Store.CreateAsync(); Assert.Equal(0, await store.CommandAsync(store.Options));
        await store.StartProductionAsync();
        var client = store.Client!;
        using var login = await client.PostAsJsonAsync("api/v1/auth/login", new { email = store.Options.Email, password = store.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new("Bearer", json.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/v1/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/v1/leave-requests/balances")).StatusCode);
        foreach (var decision in new[] { "approve", "reject" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"api/v1/leave-requests/{Guid.NewGuid()}/{decision}", new { })).StatusCode);
        await using (var read = store.Context())
        {
            var departmentId = await read.Departments.Select(d => d.Id).SingleAsync();
            using var onboarding = await client.PostAsJsonAsync("api/v1/employees", new
            {
                fullName = "Synthetic uncreated employee", email = "not-created@synthetic.invalid", departmentId,
                roles = new[] { EmployeeRoles.Employee }, employeeNumber = "UNCREATED", employmentStartDate = "2024-02-29"
            });
            Assert.Equal(HttpStatusCode.BadRequest, onboarding.StatusCode);
        }
        Assert.NotEqual(0, await store.CommandAsync(store.Options)); // Running API also owns the maintenance lease.
        await using var db = store.Context(); Assert.Equal(1, await db.Users.CountAsync());
        Assert.Empty(await db.LeaveRequests.ToArrayAsync()); Assert.Empty(await db.AuditEntries.ToArrayAsync());
    }

    private sealed class Store : IAsyncDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "hrflow-first-admin-" + Guid.NewGuid().ToString("N"));
        public string Password { get; } = "Aa!9" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        public string PasswordFile => Path.Combine(Root, "credential.private");
        public string Database => Path.Combine(Root, "fixture.sqlite");
        public HttpClient? Client { get; private set; }
        private Process? api;
        public FirstAdministratorOptions Options => new(Database, ApiFixture.RepositoryRoot, PasswordFile,
            "Synthetic first administrator", "first-admin@synthetic.invalid", " admin_001 ", "2024-02-29", null, "Initial department");
        public HRFlowDbContext Context() => new(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=" + Database + ";Foreign Keys=True;Pooling=False").Options);
        public static async Task<Store> CreateAsync(bool migrate = true)
        {
            var store = new Store();
            try
            {
                ApiFixture.CreatePrivateDirectory(store.Root); ApiFixture.CreatePrivateFile(store.Database);
                ApiFixture.CreatePrivateFile(store.PasswordFile); await File.WriteAllTextAsync(store.PasswordFile, store.Password);
                if (migrate) { await using var db = store.Context(); await db.Database.MigrateAsync(); }
                ApiFixture.AssertPrivatePath(store.Root); ApiFixture.AssertPrivatePath(store.Database);
                ApiFixture.AssertPrivatePath(store.PasswordFile);
                return store;
            }
            catch { await store.DisposeAsync(); throw; }
        }
        public async Task AssertEmptyAsync()
        {
            await using var db = Context(); Assert.Empty(await db.Users.ToArrayAsync()); Assert.Empty(await db.Employees.ToArrayAsync());
            Assert.Empty(await db.UserRoles.ToArrayAsync()); Assert.Empty(await db.Roles.ToArrayAsync()); Assert.Empty(await db.Departments.ToArrayAsync());
        }
        private static ProcessStartInfo StartInfo(string dll)
        {
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true, WorkingDirectory = ApiFixture.RepositoryRoot };
            start.ArgumentList.Add(dll); return start;
        }
        public async Task<int> CommandAsync(FirstAdministratorOptions options)
        {
            var start = StartInfo(Path.Combine(ApiFixture.RepositoryRoot, "tools/HRFlow.Operations/bin/Debug/net8.0/HRFlow.Operations.dll"));
            foreach (var value in new[] { "first-admin", "--database", options.Database, "--application-root", options.ApplicationRoot,
                "--password-file", options.PasswordFile, "--name", options.Name, "--email", options.Email, "--employee-number", options.EmployeeNumber,
                "--employment-start-date", options.EmploymentStartDate }) start.ArgumentList.Add(value);
            if (options.DepartmentId is {} id) { start.ArgumentList.Add("--department-id"); start.ArgumentList.Add(id.ToString()); }
            if (options.CreateDepartment != null) { start.ArgumentList.Add("--create-department"); start.ArgumentList.Add(options.CreateDepartment); }
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
                Assert.True(!(await output).Contains(Password, StringComparison.Ordinal), "CLI output must not contain private credentials.");
                Assert.True(!(await error).Contains(Password, StringComparison.Ordinal), "CLI errors must not contain private credentials.");
                return process.ExitCode;
            }
            finally { await StopAsync(process); }
        }
        public Process StartRace(string descriptor)
        {
            var start = StartInfo(typeof(FirstAdministratorTests).Assembly.Location);
            start.ArgumentList.Add("--first-admin-race"); start.ArgumentList.Add(descriptor);
            return Process.Start(start)!;
        }
        public void MakePasswordInsecure()
        {
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(PasswordFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            else
            {
                var security = new System.Security.AccessControl.FileSecurity(); security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(System.Security.Principal.WindowsIdentity.GetCurrent().User!,
                    System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
                security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier("S-1-1-0"),
                    System.Security.AccessControl.FileSystemRights.Read, System.Security.AccessControl.AccessControlType.Allow));
                new FileInfo(PasswordFile).SetAccessControl(security);
            }
        }
        public async Task StartProductionAsync()
        {
            var privateRoot = Path.Combine(Root, "private"); ApiFixture.CreatePrivateDirectory(privateRoot);
            using var rsa = RSA.Create(2048); var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder(); names.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(names.Build());
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            var certificatePath = Path.Combine(Root, "tls.private.pfx"); ApiFixture.CreatePrivateFile(certificatePath);
            // The account credential stays in its private file/HTTPS login, never the child environment.
            var certificatePassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Pfx, certificatePassword));
            var start = StartInfo(Path.Combine(ApiFixture.RepositoryRoot, "src/HRFlow.Api/bin/Debug/net8.0/HRFlow.Api.dll"));
            var essential = new[] { "PATH", "DOTNET_ROOT", "HOME", "USERPROFILE", "LOCALAPPDATA", "SystemRoot", "TEMP", "TMP" }.ToDictionary(k => k, Environment.GetEnvironmentVariable);
            start.Environment.Clear(); foreach (var (key, value) in essential) if (value != null) start.Environment[key] = value;
            foreach (var (key, value) in new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"]="Production", ["ASPNETCORE_URLS"]="https://127.0.0.1:0", ["AllowedHosts"]="127.0.0.1",
                ["ConnectionStrings__DefaultConnection"]="Data Source="+Database+";Mode=ReadWrite;Foreign Keys=True",
                ["Storage__LocalFileSystem"]="true", ["Storage__PrivateFilesRoot"]=privateRoot,
                ["Authentication__Jwt__Issuer"]="HRFlow.LocalVerification", ["Authentication__Jwt__Audience"]="HRFlow.LocalVerification.Client",
                ["Authentication__Jwt__SigningKey"]=Convert.ToHexString(RandomNumberGenerator.GetBytes(48)),
                ["Authentication__Jwt__RefreshTokenPepper"]=Convert.ToHexString(RandomNumberGenerator.GetBytes(48)),
                ["Activation__ApplicationOrigin"]="https://127.0.0.1", ["ASPNETCORE_Kestrel__Certificates__Default__Path"]=certificatePath,
                ["ASPNETCORE_Kestrel__Certificates__Default__Password"]=certificatePassword, ["Logging__LogLevel__Default"]="Warning",
                ["Logging__LogLevel__Microsoft.Hosting.Lifetime"]="Information"
            }) start.Environment[key]=value;
            var ready = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
            api = new Process { StartInfo = start };
            api.OutputDataReceived += (_, line) => { const string marker = "Now listening on: "; if (line.Data is {} text && text.Contains(marker)) ready.TrySetResult(new Uri(text[(text.IndexOf(marker, StringComparison.Ordinal)+marker.Length)..].Trim())); };
            api.ErrorDataReceived += (_, _) => { }; api.Start(); api.BeginOutputReadLine(); api.BeginErrorReadLine();
            var origin = await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var thumbprint = certificate.Thumbprint;
            Client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert?.Thumbprint == thumbprint })
                { BaseAddress = origin, Timeout = TimeSpan.FromSeconds(15) };
            Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("health/ready")).StatusCode);
        }
        public static async Task StopAsync(Process process)
        {
            if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        }
        public async ValueTask DisposeAsync()
        {
            if (api != null) { await StopAsync(api); api.Dispose(); }
            Client?.Dispose(); SqliteConnection.ClearAllPools();
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
