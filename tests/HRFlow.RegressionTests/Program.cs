using System.Text.Json;
using HRFlow.RegressionTests;

// Playwright uses exactly the same migrated/seeded process fixture as xUnit, not a public provisioning endpoint.
if (args.Length != 3 || args[0] != "--serve") return 2;
var directory = Path.GetFullPath(Path.GetDirectoryName(args[1])!);
if (Path.GetDirectoryName(directory) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
    || !Path.GetFileName(directory).StartsWith("hrflow-browser-", StringComparison.Ordinal)
    || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return 2;
ApiFixture.RestrictDirectory(directory);
using var stopped = new CancellationTokenSource();
var input = Task.Run(() => { Console.ReadLine(); stopped.Cancel(); });
await using var fixture = await ApiFixture.StartAsync(args[2], stopped.Token);
var pending = Path.Combine(directory, "private-state-" + Guid.NewGuid().ToString("N") + ".partial");
await File.WriteAllTextAsync(pending, JsonSerializer.Serialize(fixture.BrowserState));
ApiFixture.RestrictFile(pending);
File.Move(pending, args[1], overwrite: false);
Console.WriteLine("Disposable fixture ready.");
await input;
return 0;
