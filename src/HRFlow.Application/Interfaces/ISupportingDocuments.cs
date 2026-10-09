namespace HRFlow.Application.Interfaces;

/// <summary>Evidence persistence extends the scoped context without adding new EF surface to Domain.</summary>
public interface IApplicationDocumentContext : HRFlow.Domain.Interfaces.IApplicationDbContext
{
    Microsoft.EntityFrameworkCore.DbSet<HRFlow.Domain.Entities.SupportingDocument> SupportingDocuments { get; }
    Microsoft.EntityFrameworkCore.DbSet<HRFlow.Domain.Entities.DocumentAccessEntry> DocumentAccessEntries { get; }
}

/// <summary>Private object operations never accept caller-controlled filesystem paths.</summary>
public interface IPrivateDocumentStorage
{
    /// <summary>Bounds, validates and stages a complete object in quarantine before it can be scanned.</summary>
    Task<StoredDocument> StageAsync(Guid id, Stream input, string extension, string mediaType, CancellationToken token);
    /// <summary>Opens an immutable generated object; Application decides whether clean content is authorised.</summary>
    Stream Open(Guid id, bool clean);
    /// <summary>Creates the clean copy after an authoritative scanner verdict, without removing quarantine prematurely.</summary>
    void Promote(Guid id, Stream verifiedContent);
    /// <summary>Returns a bounded immutable byte snapshot only when its size and digest match persisted facts.</summary>
    Task<Stream> OpenVerifiedAsync(Guid id, bool clean, long size, string checksum, CancellationToken token);
    /// <summary>Deletes both copies only after a durable removal decision.</summary>
    void Remove(Guid id);
}
/// <summary>Scanner failures are distinct from a positive malware verdict; neither grants clean status.</summary>
public interface IDocumentScanner
{
    /// <summary>Returns a bounded scan verdict; absence or uncertainty must never be treated as clean.</summary>
    Task<string> ScanAsync(Stream input, CancellationToken token);
}
/// <summary>Private content facts contain no filesystem paths or user filenames.</summary>
public sealed record StoredDocument(long Size, string Checksum, string MediaType);
/// <summary>Managers receive medical status only; content metadata is explicitly nullable.</summary>
public sealed record DocumentDto(Guid Id, string Class, string Status, Guid Version, long? Size, string? MediaType, bool CanDownload);
/// <summary>Authorized bytes are transferred after database protection has ended.</summary>
public sealed record DocumentDownload(Stream Content, string MediaType);
