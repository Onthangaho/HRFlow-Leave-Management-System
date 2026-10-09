using System.Security.Cryptography;
using System.Text.Json;
using HRFlow.Infrastructure.Services.Operations;
using Microsoft.Data.Sqlite;

// This executable is a local OS-operator boundary, not an authenticated HTTP endpoint or dashboard feature.
try
{
    if (args.Length == 0) throw new InvalidDataException("An operator command is required.");
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    var switches = new HashSet<string> { "--invalidate-security", "--confirm-current-permissions" };
    for (var index = 1; index < args.Length; index++)
    {
        var name = args[index];
        if (!name.StartsWith("--", StringComparison.Ordinal) || options.ContainsKey(name)) throw new InvalidDataException("Invalid arguments.");
        if (switches.Contains(name)) options.Add(name, "true");
        else if (++index < args.Length) options.Add(name, args[index]);
        else throw new InvalidDataException("An option value is missing.");
    }
    var valid = args[0] switch
    {
        "keygen" => new[] { "--output", "--application-root" },
        "backup" => ["--database", "--private-root", "--output", "--key-file", "--application-root"],
        "restore" => ["--package", "--destination", "--key-file", "--application-root", "--invalidate-security"],
        "recover-password" => ["--database", "--identity", "--password-file", "--application-root", "--confirm-current-permissions"],
        "cleanup-staging" => ["--staging", "--application-root"],
        _ => throw new InvalidDataException("Unknown command.")
    };
    if (options.Keys.Except(valid).Any()) throw new InvalidDataException("Unknown options.");
    string Get(string name) => options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidDataException("Required option missing.");
    var root = Get("--application-root");
    if (!Path.IsPathFullyQualified(root) || !Directory.Exists(root)) throw new InvalidDataException("Existing application root is required.");
    OperationSummary? summary = null;
    switch (args[0])
    {
        case "keygen": BackupRestoreService.GenerateKey(Get("--output"), root); break;
        case "backup": summary = BackupRestoreService.Backup(Get("--database"), Get("--private-root"), Get("--output"), Get("--key-file"), root); break;
        case "restore": summary = BackupRestoreService.Restore(Get("--package"), Get("--destination"), Get("--key-file"), root, options.ContainsKey("--invalidate-security")); break;
        case "recover-password": await RecoveryPasswordService.RecoverAsync(Get("--database"), Guid.Parse(Get("--identity")), Get("--password-file"), root, options.ContainsKey("--confirm-current-permissions")); break;
        case "cleanup-staging": BackupRestoreService.DeleteStage(Get("--staging"), root); break;
    }
    Console.WriteLine(JsonSerializer.Serialize(new { outcome = "completed", command = args[0], summary }));
    return 0;
}
catch (Exception exception)
{
    var category = exception switch
    {
        CryptographicException => "authentication_failed",
        InvalidDataException or JsonException or ArgumentException => "invalid_input_or_package",
        SqliteException => "database_unavailable_or_incompatible",
        IOException or UnauthorizedAccessException or InvalidOperationException => "storage_or_quiescence_unavailable",
        _ => "operation_failed"
    };
    Console.Error.WriteLine(JsonSerializer.Serialize(new { outcome = "failed", category,
        guidance = "Check the runbook, stopped services, private paths, matching schema build and separately held key. No existing restore destination is replaced." }));
    return 1;
}
