using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Owns one sequential application lineage at a time, private keys and actual operator CLI invocations.</summary>
internal sealed class ActivationRecoveryFixture : IAsyncDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), "hrflow-activation-recovery-" + Guid.NewGuid().ToString("N"));
    internal Guid? WorkerSentinel { get; set; }
    internal string Destination => Path.Combine(Root, "restored");
    internal string Database => Path.Combine(Destination, "database.sqlite");
    internal string Key => Path.Combine(Root, "custody", "encryption.key");
    internal string Package => Path.Combine(Root, "archives", "backup.package");
    internal string PasswordFile => Path.Combine(Root, "custody", "recovery.private");
    internal PrivateActivationSmtp Smtp { get; } = new();
    internal List<string> Secrets { get; } = [];
    private readonly List<Host> hosts = [];
    internal bool Cleaned { get; private set; }
    private readonly Dictionary<string, string> authentication;
    private ActivationRecoveryFixture(Dictionary<string, string> authentication) { this.authentication = authentication; }
    internal static async Task<ActivationRecoveryFixture> CreateAsync(Dictionary<string, string> authentication)
    {
        var fixture = new ActivationRecoveryFixture(authentication);
        try
        {
            ApiFixture.CreatePrivateDirectory(fixture.Root);
            ApiFixture.CreatePrivateDirectory(Path.Combine(fixture.Root, "custody"));
            ApiFixture.CreatePrivateDirectory(Path.Combine(fixture.Root, "archives"));
            fixture.Secrets.AddRange(authentication.Values);
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }
    internal static HRFlowDbContext Context(string database) => new(new DbContextOptionsBuilder<HRFlowDbContext>()
        .UseSqlite("Data Source=" + database + ";Mode=ReadWrite;Foreign Keys=True;Pooling=False;Default Timeout=3").Options);
    internal async Task ToolAsync(string command, params string[] args)
    {
        var start = StartInfo(Path.Combine(ApiFixture.RepositoryRoot, "tools/HRFlow.Operations/bin/Debug/net8.0/HRFlow.Operations.dll"));
        foreach (var arg in new[] { command, "--application-root", ApiFixture.RepositoryRoot }.Concat(args)) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var stdout = process.StandardOutput.ReadToEndAsync(deadline.Token); var stderr = process.StandardError.ReadToEndAsync(deadline.Token);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            var output = await Task.WhenAll(stdout, stderr).WaitAsync(deadline.Token);
            AssertNoLeaks(string.Concat(output));
            Assert.Equal(0, process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"recovery-tool: exited={process.HasExited}; stdout-complete={stdout.IsCompleted}; stderr-complete={stderr.IsCompleted}");
            throw;
        }
        finally { await StopProcessAsync(process); }
    }
    internal void AssertNoLeaks(string text)
    {
        Assert.True(!Secrets.Any(s => text.Contains(s, StringComparison.Ordinal)), "Private canary must not appear in child output.");
        Assert.True(!text.Contains(Root, StringComparison.OrdinalIgnoreCase) && !text.Contains("#token=", StringComparison.Ordinal), "Output must exclude private recovery paths and invitation links.");
        Assert.True(!System.Text.RegularExpressions.Regex.IsMatch(text, @"eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+"), "JWT values must not enter output.");
        Assert.True(!new[] { "Content-Transfer-Encoding:", "multipart/alternative", "Establish your password:" }.Any(marker => text.Contains(marker, StringComparison.Ordinal)),
            "MIME contents must not enter application or operator output.");
    }
    internal void AssertPermissions()
    {
        ApiFixture.AssertPrivatePath(Root);
        foreach (var entry in Directory.EnumerateFileSystemEntries(Root, "*", SearchOption.AllDirectories)) ApiFixture.AssertPrivatePath(entry);
    }
    internal async Task<Host> StartHostAsync(string database, string privateRoot, bool production = false, bool forbiddenPickup = false)
    {
        Assert.True(hosts.All(h => h.Stopped), "Original and recovered application lineages must never overlap.");
        var host = new Host(this, database, privateRoot, authentication, production, forbiddenPickup); hosts.Add(host);
        await host.StartAsync(forbiddenPickup); return host;
    }
    private static ProcessStartInfo StartInfo(string dll)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.Combine(ApiFixture.RepositoryRoot, "src/HRFlow.Api")
        };
        var essential = new[] { "PATH", "DOTNET_ROOT", "HOME", "USERPROFILE", "LOCALAPPDATA", "SystemRoot", "TEMP", "TMP" }.ToDictionary(k => k, Environment.GetEnvironmentVariable);
        start.Environment.Clear(); foreach (var (key, value) in essential) if (value != null) start.Environment[key] = value;
        start.ArgumentList.Add(dll); return start;
    }
    private static async Task StopProcessAsync(Process process)
    {
        if (!process.HasExited) process.Kill(true);
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }
    public async ValueTask DisposeAsync()
    {
        Exception? failure = null;
        foreach (var host in hosts)
            try { await host.StopAsync(); } catch (Exception error) { failure ??= error; }
        try { await Smtp.DisposeAsync(); } catch (Exception error) { failure ??= error; }
        if (hosts.Any(h => !h.Stopped) || !Smtp.Stopped)
            throw new InvalidOperationException("Owned API/SMTP cleanup failed; private recovery store retained.");
        SqliteConnection.ClearAllPools();
        var absolute = Path.GetFullPath(Root);
        if (Path.GetDirectoryName(absolute) != Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)
            || !Path.GetFileName(absolute).StartsWith("hrflow-activation-recovery-", StringComparison.Ordinal))
            throw new InvalidOperationException("Unsafe fixture cleanup.");
        if (Directory.Exists(absolute))
        {
            if ((File.GetAttributes(absolute) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Unsafe fixture cleanup.");
            Directory.Delete(absolute, true);
        }
        Assert.True(Smtp.Stopped && hosts.All(h => h.Stopped) && !Directory.Exists(absolute), "Owned processes, SMTP tasks and private store must be cleaned.");
        Cleaned = true;
        Console.WriteLine($"recovery-cleanup: hosts={hosts.Count}; stopped={hosts.Count(h => h.Stopped)}; smtp-active={Smtp.ActiveConnections}; store-removed=True");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    /// <summary>Runs the unchanged API executable; a test-only runtime hook changes only its Development SMTP registration and observes worker diagnostics.</summary>
    internal sealed class Host
    {
        private readonly ActivationRecoveryFixture owner;
        private readonly Process process;
        private readonly FixtureProcessDiagnostics diagnostics;
        private readonly bool production;
        private bool started;
        private bool outputOverflow;
        private bool pickupRefused;
        private readonly string? thumbprint;
        private readonly List<string> output = [];
        private readonly TaskCompletionSource<Uri> listening = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource WorkerRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource WorkerCommit { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource installed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal HttpClient Client { get; private set; } = null!;
        internal bool Stopped { get; private set; }
        internal Host(ActivationRecoveryFixture owner, string database, string privateRoot, Dictionary<string, string> authentication, bool production, bool forbiddenPickup)
        {
            this.owner = owner; this.production = production;
            var start = StartInfo(Path.Combine(ApiFixture.RepositoryRoot, "src/HRFlow.Api/bin/Debug/net8.0/HRFlow.Api.dll"));
            foreach (var (key, value) in authentication) start.Environment[key] = value;
            foreach (var (key, value) in new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = production ? "Production" : "Development",
                ["ASPNETCORE_URLS"] = (production ? "https" : "http") + "://127.0.0.1:0",
                ["AllowedHosts"] = "127.0.0.1",
                ["ConnectionStrings__DefaultConnection"] = "Data Source=" + database + ";Mode=ReadWrite;Foreign Keys=True;Default Timeout=3",
                ["Storage__LocalFileSystem"] = "true",
                ["Storage__PrivateFilesRoot"] = privateRoot,
                ["Activation__ApplicationOrigin"] = production ? "https://127.0.0.1" : "http://127.0.0.1:5173",
                ["Activation__DeliveryTimeoutSeconds"] = "3",
                ["Logging__LogLevel__Default"] = "Warning",
                ["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information"
            }) start.Environment[key] = value;
            if (!production)
            {
                start.Environment["DOTNET_STARTUP_HOOKS"] = typeof(StartupHook).Assembly.Location;
                start.Environment["HRFLOW_RECOVERY_TEST_ROOT"] = owner.Root;
                start.Environment["HRFLOW_RECOVERY_TEST_SMTP"] = owner.Smtp.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (owner.WorkerSentinel is { } sentinel) start.Environment["HRFLOW_RECOVERY_TEST_EVENT"] = sentinel.ToString();
            }
            if (forbiddenPickup) start.Environment["Activation__Delivery"] = "PrivatePickup";
            if (production)
            {
                using var rsa = RSA.Create(2048); var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var names = new SubjectAlternativeNameBuilder(); names.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(names.Build());
                using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
                var file = Path.Combine(owner.Root, "custody", Guid.NewGuid().ToString("N") + ".pfx"); ApiFixture.CreatePrivateFile(file);
                var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); owner.Secrets.Add(secret); thumbprint = certificate.Thumbprint;
                File.WriteAllBytes(file, certificate.Export(X509ContentType.Pfx, secret));
                start.Environment["ASPNETCORE_Kestrel__Certificates__Default__Path"] = file; start.Environment["ASPNETCORE_Kestrel__Certificates__Default__Password"] = secret;
            }
            process = new Process { StartInfo = start };
            diagnostics = new(process);
            process.OutputDataReceived += (_, line) => Observe(line.Data); process.ErrorDataReceived += (_, line) => Observe(line.Data);
        }
        private void Observe(string? line)
        {
            if (line == null) return; lock (output) { if (output.Count < 10000) output.Add(line); else { outputOverflow = true; listening.TrySetException(new InvalidOperationException("Private log limit exceeded.")); } }
            if (line.Contains("Production invitation delivery is unavailable; remove Activation:Delivery. Development pickup is forbidden.", StringComparison.Ordinal)) pickupRefused = true;
            if (line == "test-smtp-installed") installed.TrySetResult(); if (line == "test-worker-read") WorkerRead.TrySetResult(); if (line == "test-worker-commit") WorkerCommit.TrySetResult();
            const string prefix = "Now listening on: "; if (line.Contains(prefix, StringComparison.Ordinal)) listening.TrySetResult(new Uri(line[(line.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length)..].Trim()));
        }
        internal async Task StartAsync(bool expectRefusal)
        {
            started = process.Start();
            if (!started) throw new InvalidOperationException("Owned API did not start.");
            process.BeginOutputReadLine(); process.BeginErrorReadLine();
            if (expectRefusal)
            {
                try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20)); }
                catch (TimeoutException)
                {
                    diagnostics.Write(FixtureProcessDiagnostics.Stage.RecoveryRefusal);
                    Console.WriteLine($"recovery-refusal: guard-observed={pickupRefused}; listening={listening.Task.IsCompletedSuccessfully}; exited={process.HasExited}");
                    throw;
                }
                Assert.True(diagnostics.OutputComplete, "Refused process output must reach EOF.");
                diagnostics.Write(FixtureProcessDiagnostics.Stage.RecoveryRefusal);
                Assert.NotEqual(0, process.ExitCode); Assert.True(pickupRefused, "Expected the explicit Production pickup guard.");
                Assert.False(listening.Task.IsCompletedSuccessfully);
                await StopAsync(); return;
            }
            Uri uri;
            try { uri = await listening.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch { diagnostics.Write(FixtureProcessDiagnostics.Stage.RecoveryListening); throw; }
            if (!production) await installed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate?.Thumbprint == thumbprint }) { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(15) };
            using var ready = await Client.GetAsync("health/ready"); Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
            if (!production) await WorkerRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        internal async Task<HttpResponseMessage> SendAsync(string? bearer, string route, HttpMethod? method = null, object? body = null)
        {
            using var request = new HttpRequestMessage(method ?? HttpMethod.Get, "api/v1/" + route);
            if (bearer != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (body != null) request.Content = JsonContent.Create(body);
            return await Client.SendAsync(request);
        }
        internal async Task<string> LoginAsync(string email, string password)
        {
            using var response = await SendAsync(null, "auth/login", HttpMethod.Post, new { email, password }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ApiFixture.JsonAsync(response))["accessToken"]!.GetValue<string>();
        }
        internal async Task StopAsync()
        {
            if (Stopped) return;
            if (started)
            {
                try { await StopProcessAsync(process); Assert.True(diagnostics.OutputComplete, "Owned process output must drain before disposal."); }
                catch { diagnostics.Write(FixtureProcessDiagnostics.Stage.RecoveryStop); throw; }
            }
            Client?.Dispose(); process.Dispose(); Stopped = true;
            lock (output) { Assert.False(outputOverflow); owner.AssertNoLeaks(string.Join('\n', output)); }
        }
    }
}
