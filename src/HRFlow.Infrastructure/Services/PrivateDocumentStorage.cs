using HRFlow.Domain.Entities;
using System.Security.Cryptography;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Services.Operations;
using Microsoft.Extensions.Configuration;
using PdfSharp.Pdf.IO;
using SkiaSharp;

namespace HRFlow.Infrastructure.Services;

/// <summary>Generated identifiers address restricted quarantine/clean objects; no caller path or filename is persisted.</summary>
public sealed class PrivateDocumentStorage : IPrivateDocumentStorage
{
    private static readonly uint[] PngCrcTable = Enumerable.Range(0, 256).Select(value =>
    {
        var crc = (uint)value;
        for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? 0xedb88320U ^ (crc >> 1) : crc >> 1;
        return crc;
    }).ToArray();
    private readonly string root;
    private readonly long limit;
    private bool initialized;
    private static readonly TimeSpan PublicationLockTimeout = TimeSpan.FromSeconds(3);
    private const int PublicationLockRetryMilliseconds = 25;
    /// <summary>Uses the already configured private root and refuses out-of-policy upload limits.</summary>
    public PrivateDocumentStorage(IConfiguration configuration)
    {
        root = configuration["Storage:PrivateFilesRoot"] ?? "";
        limit = configuration.GetValue<long?>("Documents:MaxBytes") ?? SupportingDocumentLimits.MaxFileBytes;
        if (limit is < 1 or > SupportingDocumentLimits.MaxFileBytes) throw new InvalidOperationException("Document limit must be positive and at most 10 MiB.");
    }
    private void Initialize()
    {
        if (initialized) return;
        if (!Path.IsPathFullyQualified(root) || !Directory.Exists(root)) throw new IOException("Provision private document storage first.");
        PrivateOperationsPaths.Check(root, Directory.GetCurrentDirectory());
        foreach (var area in new[] { "staging", "quarantine", "clean", "locks", "removed" })
        {
            var directory = Path.Combine(root, "documents", area);
            Validate(directory); // Check existing ancestors before creating anything through a substituted directory.
            Directory.CreateDirectory(directory);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Validate(directory);
        }
        initialized = true;
    }
    /// <inheritdoc />
    public async Task<StoredDocument> StageAsync(Guid id, Stream input, string extension, string mediaType, CancellationToken token)
    {
        Initialize();
        var temporary = Location(id, "staging");
        using var memory = new MemoryStream(); var buffer = new byte[81920]; int count;
        while ((count = await input.ReadAsync(buffer, token)) != 0)
        { if (memory.Length + count > limit) throw new ArgumentException("Document exceeds the configured file limit."); await memory.WriteAsync(buffer.AsMemory(0, count), token); }
        var bytes = memory.ToArray();
        ValidateContent(bytes, extension.ToLowerInvariant(), mediaType.ToLowerInvariant());
        try
        {
            using var publication = AcquirePublication(id);
            RequireNotRemoved(id);
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            { await file.WriteAsync(bytes, token); await file.FlushAsync(token); }
            File.Move(temporary, Location(id, "quarantine"), false);
            return new(bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)), mediaType.ToLowerInvariant());
        }
        finally { CryptographicOperations.ZeroMemory(bytes); if (File.Exists(temporary)) File.Delete(temporary); }
    }
    /// <inheritdoc />
    public Stream Open(Guid id, bool clean) => new FileStream(Location(id, clean ? "clean" : "quarantine"), FileMode.Open, FileAccess.Read, FileShare.Read);
    /// <inheritdoc />
    public void Promote(Guid id, Stream verifiedContent)
    {
        using var publication = AcquirePublication(id);
        RequireNotRemoved(id);
        verifiedContent.Position = 0;
        using var destination = new FileStream(Location(id, "clean"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        verifiedContent.CopyTo(destination);
        destination.Flush(true);
    }
    /// <inheritdoc />
    public async Task<Stream> OpenVerifiedAsync(Guid id, bool clean, long size, string checksum, CancellationToken token)
    {
        if (size is < 1 or > SupportingDocumentLimits.MaxFileBytes) throw new IOException("Private content integrity check failed.");
        using var file = Open(id, clean);
        if (file.Length != size) throw new IOException("Private content integrity check failed.");
        // A bounded immutable snapshot prevents a changed path or concurrent filesystem write from altering scanned/downloaded bytes.
        var bytes = new byte[(int)size];
        await file.ReadExactlyAsync(bytes, token);
        if (!StringComparer.Ordinal.Equals(Convert.ToHexString(SHA256.HashData(bytes)), checksum))
        { CryptographicOperations.ZeroMemory(bytes); throw new IOException("Private content integrity check failed."); }
        return new MemoryStream(bytes, false);
    }
    /// <inheritdoc />
    public void Remove(Guid id)
    {
        using var publication = AcquirePublication(id);
        // This persistent tombstone and every publication share an OS lock, including across API/worker processes.
        // Never delete the lock file or tombstone: a delayed publisher must observe the same exclusion identity.
        using (var marker = new FileStream(Location(id, "removed"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) marker.Flush(true);
        foreach (var area in new[] { "staging", "quarantine", "clean" })
        { var path = Location(id, area); if (File.Exists(path)) File.Delete(path); }
    }
    private FileStream AcquirePublication(Guid id)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try { return new FileStream(Location(id, "locks"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (started.Elapsed < PublicationLockTimeout) { Thread.Sleep(PublicationLockRetryMilliseconds); }
        }
    }
    private void RequireNotRemoved(Guid id)
    {
        if (File.Exists(Location(id, "removed"))) throw new IOException("Document publication was revoked.");
    }
    private string Location(Guid id, string area)
    { Initialize(); var path = Path.Combine(root, "documents", area, id.ToString("N") + ".blob"); Validate(path); return path; }
    private static void Validate(string path)
    {
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(path)!); directory != null; directory = directory.Parent)
            if (directory.Exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Unsafe private object location.");
        if ((File.Exists(path) || Directory.Exists(path)) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Unsafe private object location.");
    }
    private static void ValidateContent(byte[] bytes, string extension, string mime)
    {
        try
        {
            if (bytes.Length == 0) throw new InvalidDataException();
            if (extension == ".pdf" && mime == "application/pdf" && bytes.AsSpan().StartsWith("%PDF-"u8)
                && System.Text.Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 1024))).TrimEnd().EndsWith("%%EOF", StringComparison.Ordinal))
            {
                // Do not accept the reader's default repair/ignore behaviour for broken cross references or streams.
                var tail = System.Text.Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 1024)));
                var marker = tail.LastIndexOf("startxref", StringComparison.Ordinal);
                if (marker < 0 || !int.TryParse(tail[(marker + 9)..].Split("%%EOF")[0].Trim(), out var offset)
                    || offset <= 0 || offset >= bytes.Length) throw new InvalidDataException();
                var target = System.Text.Encoding.ASCII.GetString(bytes.AsSpan(offset, Math.Min(512, bytes.Length - offset)));
                if (!target.StartsWith("xref", StringComparison.Ordinal)
                    && !System.Text.RegularExpressions.Regex.IsMatch(target, @"^\d+\s+\d+\s+obj\s*<<[\s\S]*?/Type\s*/XRef\b", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                    throw new InvalidDataException();
                var options = new PdfReaderOptions
                {
                    InvalidStreamLength = PdfReaderProblemBehavior.ThrowException,
                    ReferenceToUndefinedObject = PdfReaderProblemBehavior.ThrowException,
                    IssuesWithDecryption = PdfReaderProblemBehavior.ThrowException,
                    ReaderProblemCallback = _ => throw new InvalidDataException()
                };
                using var document = PdfReader.Open(new MemoryStream(bytes, false), PdfDocumentOpenMode.Import, options);
                if (document.PageCount < 1 || document.PageCount > SupportingDocumentLimits.MaxPdfPages) throw new InvalidDataException(); return;
            }
            var png = extension == ".png" && mime == "image/png" && bytes.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10})
                && bytes.Length >= 20 && bytes.AsSpan(bytes.Length - 8, 4).SequenceEqual("IEND"u8);
            var jpeg = extension is ".jpg" or ".jpeg" && mime == "image/jpeg" && bytes.Length >= 4
                && bytes[0] == 255 && bytes[1] == 216 && bytes[^2] == 255 && bytes[^1] == 217;
            if (!png && !jpeg) throw new InvalidDataException();
            if (png) ValidatePngChunks(bytes);
            using var data = SKData.CreateCopy(bytes); using var codec = SKCodec.Create(data);
            if (codec == null || (long)codec.Info.Width * codec.Info.Height > SupportingDocumentLimits.MaxImagePixels || codec.FrameCount > 1) throw new InvalidDataException();
            using var image = new SKBitmap(codec.Info);
            if (codec.GetPixels(codec.Info, image.GetPixels()) != SKCodecResult.Success) throw new InvalidDataException();
        }
        catch { throw new ArgumentException("Use a complete, valid PDF, JPEG or PNG matching its extension and MIME type."); }
    }

    private static void ValidatePngChunks(byte[] bytes)
    {
        // Decoders may tolerate corrupt ancillary CRCs. Reject them instead of accepting a repaired image.
        var offset = 8;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12) throw new InvalidDataException();
            var length = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > bytes.Length - offset - 12) throw new InvalidDataException();
            var size = (int)length;
            var crc = uint.MaxValue;
            for (var index = offset + 4; index < offset + 8 + size; index++) crc = PngCrcTable[(crc ^ bytes[index]) & 255] ^ (crc >> 8);
            if (~crc != System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + size, 4))) throw new InvalidDataException();
            if (offset == 8 && (size != 13 || !bytes.AsSpan(offset + 4, 4).SequenceEqual("IHDR"u8))) throw new InvalidDataException();
            if (bytes.AsSpan(offset + 4, 4).SequenceEqual("IEND"u8) && (size != 0 || offset + 12 != bytes.Length)) throw new InvalidDataException();
            offset += 12 + size;
        }
    }
}
