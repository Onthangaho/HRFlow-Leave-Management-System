using HRFlow.Domain.Entities;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services.Operations;

/// <summary>Bounded offline packages use SQLite's backup API and authenticated .NET AES-256-GCM, not live file copies.</summary>
public static class BackupRestoreService
{
    private const int FormatVersion = 1;
    private const int MaxBytes = 100 * 1024 * 1024;
    private const int MaxEntries = 1000;
    private const string DatabaseName = "database.sqlite";
    private const string Marker = "operations.staging";
    private static readonly byte[] Header = Encoding.ASCII.GetBytes("HRFLOW-BACKUP-1\n");
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    /// <summary>Creates a random key only in an existing restricted directory and never overwrites an artifact.</summary>
    public static void GenerateKey(string output, string applicationRoot)
    {
        var path = PrivateOperationsPaths.Check(output, applicationRoot, false);
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var key = RandomNumberGenerator.GetBytes(32);
        try { file.Write(key); file.Flush(true); RestrictFile(path); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <summary>Requires stopped cooperating writers, then reserves SQLite writes throughout database/file capture.</summary>
    public static OperationSummary Backup(string database, string privateRoot, string output, string keyFile, string applicationRoot)
    {
        var timer = Stopwatch.StartNew();
        database = PrivateOperationsPaths.Check(database, applicationRoot);
        privateRoot = PrivateOperationsPaths.Check(privateRoot, applicationRoot);
        output = PrivateOperationsPaths.Check(output, applicationRoot, false);
        keyFile = PrivateOperationsPaths.Check(keyFile, applicationRoot);
        if (!Directory.Exists(privateRoot) || File.Exists(output) || Directory.Exists(output)
            || PrivateOperationsPaths.Within(database, privateRoot) || PrivateOperationsPaths.Within(output, privateRoot)
            || PrivateOperationsPaths.Within(keyFile, privateRoot) || output == database || output == keyFile || database == keyFile)
            throw new InvalidDataException("Input/output overlap or existing destination is unsupported.");
        WriterQuiescence.RequireStopped();
        using var lease = MaintenanceLease.ForOperator(database);
        using var guard = Open(database, SqliteOpenMode.ReadWrite);
        using var reservation = guard.BeginTransaction(deferred: false);
        var cutoff = DateTimeOffset.UtcNow;
        var stage = NewStage(Path.GetDirectoryName(output)!, "backup");
        try
        {
            var snapshot = Path.Combine(stage, DatabaseName);
            using (var source = Open(database, SqliteOpenMode.ReadOnly))
            using (var target = Open(snapshot, SqliteOpenMode.ReadWriteCreate)) source.BackupDatabase(target);
            CheckDatabase(snapshot);
            CheckDocuments(snapshot, privateRoot);
            var schema = ReadSchema(snapshot);
            var inventory = new List<InventoryFile>();
            using var archiveBytes = new MemoryStream();
            using (var archive = new ZipArchive(archiveBytes, ZipArchiveMode.Create, true))
            {
                AddFile(archive, snapshot, DatabaseName, inventory);
                foreach (var file in EnumerateFiles(privateRoot, applicationRoot).Order(StringComparer.Ordinal))
                    AddFile(archive, file, "private/" + Path.GetRelativePath(privateRoot, file).Replace('\\', '/'), inventory);
                var manifest = new PackageManifest(FormatVersion, "HRFlow", typeof(BackupRestoreService).Assembly.GetName().Version!.ToString(),
                    cutoff, DateTimeOffset.UtcNow, schema, inventory.ToArray());
                using var stream = archive.CreateEntry("manifest.json", CompressionLevel.NoCompression).Open();
                JsonSerializer.Serialize(stream, manifest, Json);
            }
            if (archiveBytes.Length > MaxBytes) throw new InvalidDataException("Package exceeds supported bounded size.");
            byte[] encrypted;
            try { encrypted = Encrypt(archiveBytes.ToArray(), ReadKey(keyFile)); }
            finally { CryptographicOperations.ZeroMemory(archiveBytes.GetBuffer()); }
            var temporary = Path.Combine(stage, "encrypted.package");
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { file.Write(encrypted); file.Flush(true); }
            RestrictFile(temporary);
            File.Move(temporary, output, false);
            return new(cutoff, DateTimeOffset.UtcNow, timer.Elapsed.TotalSeconds, inventory.Count, inventory.Sum(f => f.Size), 0);
        }
        finally { DeleteStage(stage, applicationRoot); }
    }

    /// <summary>Authenticates before extracting; validates all data before explicit credential invalidation and atomic publish.</summary>
    public static OperationSummary Restore(string package, string destination, string keyFile, string applicationRoot, bool invalidateSecurity)
    {
        if (!invalidateSecurity) throw new InvalidDataException("Restore requires explicit --invalidate-security; restored passwords/sessions/invitations cannot be trusted.");
        var timer = Stopwatch.StartNew();
        package = PrivateOperationsPaths.Check(package, applicationRoot);
        keyFile = PrivateOperationsPaths.Check(keyFile, applicationRoot);
        destination = PrivateOperationsPaths.Check(destination, applicationRoot, false);
        if (Path.GetFileName(destination).StartsWith("hrflow-restore-", StringComparison.Ordinal)
            || Path.GetFileName(destination).StartsWith("hrflow-backup-", StringComparison.Ordinal)
            || File.Exists(destination) || Directory.Exists(destination) || PrivateOperationsPaths.Within(package, destination)
            || PrivateOperationsPaths.Within(keyFile, destination) || package == keyFile)
            throw new InvalidDataException("Restore requires a new isolated destination.");
        var encryptedLength = new FileInfo(package).Length;
        if (encryptedLength < Header.Length + 28 || encryptedLength > MaxBytes + Header.Length + 28)
            throw new InvalidDataException("Invalid or oversized package.");
        var plaintext = Decrypt(File.ReadAllBytes(package), ReadKey(keyFile));
        var stage = NewStage(Path.GetDirectoryName(destination)!, "restore");
        try
        {
            using var memory = new MemoryStream(plaintext, false);
            using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
            if (archive.Entries.Count is < 2 or > MaxEntries + 1 || archive.Entries.Sum(e => e.Length) > MaxBytes)
                throw new InvalidDataException("Invalid archive limits.");
            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in archive.Entries)
            {
                ValidateEntryName(entry.FullName);
                if (!entries.TryAdd(entry.FullName, entry) || (entry.ExternalAttributes >> 16 & 0xF000) == 0xA000)
                    throw new InvalidDataException("Duplicate or linked archive entry.");
            }
            if (!entries.TryGetValue("manifest.json", out var manifestEntry) || manifestEntry.Length > 1024 * 1024)
                throw new InvalidDataException("Manifest missing or oversized.");
            PackageManifest manifest;
            using (var stream = manifestEntry.Open()) manifest = JsonSerializer.Deserialize<PackageManifest>(stream, Json)
                ?? throw new InvalidDataException("Manifest missing.");
            if (manifest.FormatVersion != FormatVersion || manifest.Application != "HRFlow" || string.IsNullOrWhiteSpace(manifest.ToolVersion)
                || manifest.SnapshotCutoffUtc.Offset != TimeSpan.Zero || manifest.CreatedUtc.Offset != TimeSpan.Zero
                || manifest.CreatedUtc < manifest.SnapshotCutoffUtc || manifest.Files == null || manifest.SchemaMigrations == null
                || manifest.Files.Length != entries.Count - 1 || manifest.Files.Length > MaxEntries
                || !manifest.SchemaMigrations.SequenceEqual(ExpectedSchema())) throw new InvalidDataException("Manifest/version/schema mismatch.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in manifest.Files)
            {
                ValidateEntryName(file.Path);
                if (file.Path != DatabaseName && !file.Path.StartsWith("private/", StringComparison.Ordinal)
                    || !names.Add(file.Path) || !entries.TryGetValue(file.Path, out var entry) || file.Size != entry.Length
                    || file.Size < 0 || file.Size > MaxBytes || file.Sha256 == null || file.Sha256.Length != 64)
                    throw new InvalidDataException("Inventory mismatch or missing file.");
                var path = Path.Combine(stage, file.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                RestrictDirectory(Path.GetDirectoryName(path)!);
                using (var input = entry.Open())
                using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    CopyBounded(input, output, file.Size);
                    output.Flush(true);
                }
                RestrictFile(path);
                if (!Hash(path).Equals(file.Sha256, StringComparison.Ordinal)) throw new InvalidDataException("Checksum mismatch.");
            }
            if (!names.Contains(DatabaseName)) throw new InvalidDataException("Database missing.");
            var database = Path.Combine(stage, DatabaseName);
            Directory.CreateDirectory(Path.Combine(stage, "private")); RestrictDirectory(Path.Combine(stage, "private"));
            CheckDatabase(database);
            CheckDocuments(database, Path.Combine(stage, "private"));
            if (!ReadSchema(database).SequenceEqual(manifest.SchemaMigrations)) throw new InvalidDataException("Database schema mismatch.");
            var invalidated = InvalidateSecurity(database);
            CheckDatabase(database);
            var summary = new OperationSummary(manifest.SnapshotCutoffUtc, DateTimeOffset.UtcNow, timer.Elapsed.TotalSeconds,
                manifest.Files.Length, manifest.Files.Sum(f => f.Size), invalidated);
            File.WriteAllText(Path.Combine(stage, "recovery-summary.json"), JsonSerializer.Serialize(summary));
            RestrictFile(Path.Combine(stage, "recovery-summary.json"));
            Directory.Move(stage, destination);
            // The marker is deliberately retained: explicit cleanup may remove interrupted staging paths only, never this destination.
            return summary;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (Directory.Exists(stage)) DeleteStage(stage, applicationRoot);
        }
    }

    /// <summary>Deletes only a named, marked, restricted tool staging directory; never an existing source or published restore.</summary>
    public static void DeleteStage(string stage, string applicationRoot)
    {
        stage = PrivateOperationsPaths.Check(stage, applicationRoot);
        var name = Path.GetFileName(stage);
        if (!(name.StartsWith("hrflow-backup-", StringComparison.Ordinal) || name.StartsWith("hrflow-restore-", StringComparison.Ordinal))
            || !Guid.TryParseExact(name[(name.LastIndexOf('-') + 1)..], "N", out _)
            || File.ReadAllText(Path.Combine(stage, Marker)) != "HRFlowOperationsV1") throw new InvalidDataException("Not an owned staging directory.");
        _ = EnumerateFiles(stage, applicationRoot).ToArray();
        if (File.Exists(Path.Combine(stage, DatabaseName)))
        {
            using var lease = MaintenanceLease.ForOperator(Path.Combine(stage, DatabaseName));
        }
        Directory.Delete(stage, true);
    }

    private static int InvalidateSecurity(string database)
    {
        using var connection = Open(database, SqliteOpenMode.ReadWrite);
        using var transaction = connection.BeginTransaction(deferred: false);
        using var read = connection.CreateCommand(); read.Transaction = transaction; read.CommandText = "SELECT Id FROM AspNetUsers";
        var ids = new List<string>(); using (var reader = read.ExecuteReader()) while (reader.Read()) ids.Add(reader.GetString(0));
        foreach (var id in ids)
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "UPDATE AspNetUsers SET PasswordHash=NULL, CredentialVersion=$version, SecurityStamp=$stamp, ActivationTokenHash=NULL, ActivationExpiresAtUtc=NULL, InvitationDeliveryState=CASE WHEN RequiresActivation=1 THEN 'Expired' ELSE InvitationDeliveryState END WHERE Id=$id";
            command.Parameters.AddWithValue("$version", Guid.NewGuid().ToString()); command.Parameters.AddWithValue("$stamp", Guid.NewGuid().ToString()); command.Parameters.AddWithValue("$id", id); command.ExecuteNonQuery();
        }
        using var revoke = connection.CreateCommand(); revoke.Transaction = transaction;
        revoke.CommandText = "UPDATE RefreshTokens SET RevokedAtUtc=$now WHERE RevokedAtUtc IS NULL";
        revoke.Parameters.AddWithValue("$now", DateTime.UtcNow); revoke.ExecuteNonQuery();
        transaction.Commit(); return ids.Count;
    }

    private static string NewStage(string parent, string operation)
    {
        var stage = Path.Combine(parent, "hrflow-" + operation + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage); RestrictDirectory(stage);
        File.WriteAllText(Path.Combine(stage, Marker), "HRFlowOperationsV1"); RestrictFile(Path.Combine(stage, Marker));
        return stage;
    }

    private static IEnumerable<string> EnumerateFiles(string root, string applicationRoot)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(root))
        {
            PrivateOperationsPaths.Check(entry, applicationRoot);
            if (Directory.Exists(entry)) foreach (var file in EnumerateFiles(entry, applicationRoot)) yield return file;
            else yield return entry;
        }
    }

    private static void AddFile(ZipArchive archive, string path, string name, List<InventoryFile> inventory)
    {
        ValidateEntryName(name);
        var size = new FileInfo(path).Length;
        if (inventory.Count >= MaxEntries || size + inventory.Sum(f => f.Size) > MaxBytes) throw new InvalidDataException("Inventory exceeds supported size.");
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var output = archive.CreateEntry(name, CompressionLevel.NoCompression).Open()) CopyBounded(input, output, size);
        inventory.Add(new(name, size, Hash(path)));
    }

    private static void CopyBounded(Stream input, Stream output, long expected)
    {
        var buffer = new byte[81920]; long copied = 0; int count;
        while ((count = input.Read(buffer)) != 0)
        { copied += count; if (copied > expected || copied > MaxBytes) throw new InvalidDataException("Entry size mismatch."); output.Write(buffer, 0, count); }
        if (copied != expected) throw new InvalidDataException("Entry truncated.");
    }

    private static void ValidateEntryName(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Contains('\\') || path.StartsWith('/') || path.Any(char.IsControl)
            || path.Split('/').Any(segment => segment is "" or "." or ".."
                || segment.IndexOfAny([':', '<', '>', '"', '|', '?', '*']) >= 0
                || segment.EndsWith('.') || segment.EndsWith(' ')
                || IsDeviceName(segment)))
            throw new InvalidDataException("Unsafe archive path.");
    }

    // Reject Windows aliases even on Linux so a portable archive cannot acquire a different meaning on restore.
    private static bool IsDeviceName(string segment)
    {
        var stem = segment.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL"
            || stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                && (stem[3] is >= '1' and <= '9' or '¹' or '²' or '³');
    }

    private static string Hash(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)); }
    private static byte[] ReadKey(string path) { if (new FileInfo(path).Length != 32) throw new InvalidDataException("Key must contain exactly 32 random bytes."); return File.ReadAllBytes(path); }
    private static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        try
        {
            var result = new byte[Header.Length + 28 + plaintext.Length]; Header.CopyTo(result, 0);
            var nonce = result.AsSpan(Header.Length, 12); RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, result.AsSpan(Header.Length + 28), result.AsSpan(Header.Length + 12, 16), Header);
            return result;
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plaintext); }
    }
    private static byte[] Decrypt(byte[] encrypted, byte[] key)
    {
        var result = new byte[encrypted.Length - Header.Length - 28];
        try
        {
            if (!encrypted.AsSpan(0, Header.Length).SequenceEqual(Header)) throw new InvalidDataException("Unknown package format.");
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(encrypted.AsSpan(Header.Length, 12), encrypted.AsSpan(Header.Length + 28), encrypted.AsSpan(Header.Length + 12, 16), result, Header);
            return result;
        }
        catch { CryptographicOperations.ZeroMemory(result); throw; }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, ForeignKeys = true, DefaultTimeout = 3, Pooling = false }.ToString());
        try { connection.Open(); return connection; } catch { connection.Dispose(); throw; }
    }
    private static string[] ExpectedSchema()
    {
        using var context = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=:memory:").Options);
        return context.Database.GetMigrations().ToArray();
    }
    private static string[] ReadSchema(string path)
    {
        using var connection = Open(path, SqliteOpenMode.ReadOnly);
        using var command = connection.CreateCommand(); command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";
        var ids = new List<string>(); using var reader = command.ExecuteReader(); while (reader.Read()) ids.Add(reader.GetString(0));
        if (!ids.SequenceEqual(ExpectedSchema())) throw new InvalidDataException("Unsupported schema; use the matching application/tool build, never auto-migrate recovery data.");
        return ids.ToArray();
    }
    private static void CheckDatabase(string path)
    {
        using var connection = Open(path, SqliteOpenMode.ReadOnly);
        using var check = connection.CreateCommand(); check.CommandText = "PRAGMA integrity_check";
        using (var reader = check.ExecuteReader()) { if (!reader.Read() || reader.GetString(0) != "ok" || reader.Read()) throw new InvalidDataException("SQLite integrity check failed."); }
        check.CommandText = "PRAGMA foreign_key_check";
        using (var foreignKeys = check.ExecuteReader())
            if (foreignKeys.Read()) throw new InvalidDataException("SQLite reference check failed.");
        using var context = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=:memory:").Options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        var expectedConnection = (SqliteConnection)context.Database.GetDbConnection();
        foreach (var table in context.Model.GetRelationalModel().Tables)
        {
            check.CommandText = "PRAGMA table_info(\"" + table.Name.Replace("\"", "\"\"") + "\")";
            var columns = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var reader = check.ExecuteReader())
                while (reader.Read()) columns.Add(reader.GetString(1), reader.GetString(2));
            if (columns.Count != table.Columns.Count() || table.Columns.Any(column =>
                !columns.TryGetValue(column.Name, out var type) || !type.Equals(column.StoreType, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("SQLite tables/columns do not match this application build.");
            if (!ReadConstraints(connection, table.Name).SequenceEqual(ReadConstraints(expectedConnection, table.Name)))
                throw new InvalidDataException("SQLite keys, nullability or indexes do not match this application build.");
        }
    }

    // Blob checks complement SQLite integrity: relational integrity alone cannot prove evidence recoverability.
    private static void CheckDocuments(string database, string privateRoot)
    {
        using var connection = Open(database, SqliteOpenMode.ReadOnly);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Status, Size, Checksum FROM SupportingDocuments WHERE Status NOT IN ('Receiving','Removed')";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!Guid.TryParse(reader.GetString(0), out var id)) throw new InvalidDataException("Invalid document identifier.");
            var area = reader.GetString(1) == SupportingDocumentStatus.Clean ? "clean" : "quarantine";
            var path = Path.Combine(privateRoot, "documents", area, id.ToString("N") + ".blob");
            if (!File.Exists(path) || new FileInfo(path).Length != reader.GetInt64(2) || Hash(path) != reader.GetString(3))
                throw new InvalidDataException("Required private document content is missing or inconsistent. Reconcile the evidence store before recovery.");
        }
    }

    private static string[] ReadConstraints(SqliteConnection connection, string table)
    {
        var shape = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"" + table.Replace("\"", "\"\"") + "\")";
        using (var reader = command.ExecuteReader())
            while (reader.Read()) shape.Add($"column:{reader.GetString(1)}:{reader.GetInt32(3)}:{reader.GetInt32(5)}");
        command.CommandText = "PRAGMA foreign_key_list(\"" + table.Replace("\"", "\"\"") + "\")";
        using (var reader = command.ExecuteReader())
            while (reader.Read()) shape.Add("fk:" + JsonSerializer.Serialize(Enumerable.Range(2, 6).Select(reader.GetValue)));
        var indexes = new List<(string Name, int Unique, string Origin, int Partial)>();
        command.CommandText = "PRAGMA index_list(\"" + table.Replace("\"", "\"\"") + "\")";
        using (var reader = command.ExecuteReader())
            while (reader.Read()) indexes.Add((reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetInt32(4)));
        foreach (var index in indexes)
        {
            command.CommandText = "PRAGMA index_xinfo(\"" + index.Name.Replace("\"", "\"\"") + "\")";
            var fields = new List<string>();
            using (var reader = command.ExecuteReader())
                while (reader.Read()) fields.Add(JsonSerializer.Serialize(Enumerable.Range(2, 4).Select(reader.GetValue)));
            shape.Add($"index:{index.Unique}:{index.Origin}:{index.Partial}:" + string.Join(";", fields));
        }
        return shape.Order(StringComparer.Ordinal).ToArray();
    }
    private static void RestrictDirectory(string path) { if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
    private static void RestrictFile(string path) { if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }

    private sealed record PackageManifest(int FormatVersion, string Application, string ToolVersion, DateTimeOffset SnapshotCutoffUtc,
        DateTimeOffset CreatedUtc, string[] SchemaMigrations, InventoryFile[] Files);
    private sealed record InventoryFile(string Path, long Size, string Sha256);
}

/// <summary>Safe operational timings/counts; never includes paths, keys, credentials or file names.</summary>
public sealed record OperationSummary(DateTimeOffset SnapshotCutoffUtc, DateTimeOffset CompletedUtc, double ElapsedSeconds,
    int InventoryCount, long InventoryBytes, int SecurityAccountsInvalidated);
