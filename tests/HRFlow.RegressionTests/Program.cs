using System.Text.Json;
using HRFlow.RegressionTests;
using Microsoft.Extensions.Configuration;

// Test-only stdin barrier starts independent real provisioning services, never a production bypass.
if (args.Length == 2 && args[0] == "--first-admin-race")
{
    var descriptor = Path.GetFullPath(args[1]);
    var parent = Path.GetDirectoryName(descriptor)!;
    if (Path.GetDirectoryName(parent) != Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)
        || !Path.GetFileName(parent).StartsWith("hrflow-first-admin-", StringComparison.Ordinal)) return 2;
    HRFlow.Infrastructure.Services.Operations.PrivateOperationsPaths.Check(descriptor, ApiFixture.RepositoryRoot);
    var options = JsonSerializer.Deserialize<HRFlow.Infrastructure.Services.Operations.FirstAdministratorOptions>(await File.ReadAllTextAsync(descriptor))!;
    Console.WriteLine("Ready");
    await Task.Run(Console.ReadLine).WaitAsync(TimeSpan.FromSeconds(15));
    try { await HRFlow.Infrastructure.Services.Operations.FirstAdministratorService.ProvisionAsync(options); return 0; }
    catch { return 1; }
}

// Separate OS processes exercise publication/removal; this fixture command is never an API or production seam.
if (args.Length == 3 && args[0] is "--publish-image" or "--remove-image")
{
    var root = Path.GetFullPath(args[1]);
    if (Path.GetDirectoryName(root) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
        || !Path.GetFileName(root).StartsWith("hrflow-image-test-", StringComparison.Ordinal)
        || !Guid.TryParse(args[2], out var id)) return 2;
    var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:PrivateFilesRoot"] = root }).Build();
    var storage = new HRFlow.Infrastructure.Services.PrivateProfileImageStorage(configuration);
    if (args[0] == "--remove-image") { storage.Remove(id); Console.WriteLine("Removed"); return 0; }
    using var imageInput = new MemoryStream(ProfileImageTests.Raster());
    var output = await storage.ProcessAsync(imageInput, ".png", "image/png", new(0, 0, 1), default);
    Console.WriteLine("Ready");
    await Task.Run(Console.ReadLine).WaitAsync(TimeSpan.FromSeconds(10));
    try { await storage.PublishAsync(id, output, default); Console.WriteLine("Published"); return 3; }
    catch (IOException) { Console.WriteLine("Revoked"); return 0; }
}

// Playwright uses exactly the same migrated/seeded process fixture as xUnit, not a public provisioning endpoint.
if (args.Length != 3 || args[0] != "--serve") return 2;
var directory = Path.GetFullPath(Path.GetDirectoryName(args[1])!);
if (Path.GetDirectoryName(directory) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
    || !Path.GetFileName(directory).StartsWith("hrflow-browser-", StringComparison.Ordinal)
    || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return 2;
ApiFixture.RestrictDirectory(directory);
using var stopped = new CancellationTokenSource();
var input = Task.Run(() => { Console.ReadLine(); stopped.Cancel(); });
await using var fixture = await ApiFixture.StartAsync(args[2], stopped.Token,
    int.TryParse(Environment.GetEnvironmentVariable("HRFLOW_TEST_CLAMAV_PORT"), out var scannerPort) ? scannerPort : 0);
var directoryRows = int.TryParse(Environment.GetEnvironmentVariable("HRFLOW_DIRECTORY_FIXTURE_ROWS"), out var configuredRows) ? configuredRows : 75;
if (directoryRows is > 0 and <= 1000)
    await DirectoryFixtureData.SeedAsync(fixture, directoryRows);
var pending = Path.Combine(directory, "private-state-" + Guid.NewGuid().ToString("N") + ".partial");
await File.WriteAllTextAsync(pending, JsonSerializer.Serialize(fixture.BrowserState));
ApiFixture.RestrictFile(pending);
File.Move(pending, args[1], overwrite: false);
Console.WriteLine("Disposable fixture ready.");
await input;
return 0;
