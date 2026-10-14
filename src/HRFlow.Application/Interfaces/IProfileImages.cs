using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Interfaces;

/// <summary>Image metadata is independent of evidence classification and request binding.</summary>
public interface IProfileImageContext : HRFlow.Domain.Interfaces.IApplicationDbContext
{
    DbSet<ProfileImage> ProfileImages { get; }
}

/// <summary>Fractions describe a square in the orientation-corrected image, never filesystem coordinates.</summary>
public sealed record ImageCrop(double X, double Y, double Size);
/// <summary>Only a committed processed image may be downloaded.</summary>
public sealed record ProfileImageDto(Guid Version, bool HasImage);
/// <summary>Processing and immutable publication occur outside the database writer.</summary>
public interface IProfileImageStorage
{
    /// <summary>Bounds decoding and strips source metadata before returning the only bytes eligible for scanning.</summary>
    Task<byte[]> ProcessAsync(Stream input, string extension, string mime, ImageCrop crop, CancellationToken token);
    /// <summary>Publishes immutable processed bytes only if the permanent removal marker permits it.</summary>
    Task<StoredDocument> PublishAsync(Guid id, byte[] output, CancellationToken token);
    /// <summary>Returns an immutable bounded content snapshot only when persisted integrity facts match.</summary>
    Task<Stream> OpenVerifiedAsync(Guid id, long size, string checksum, CancellationToken token);
    /// <summary>Installs a permanent cross-process tombstone before deleting any image bytes.</summary>
    void Remove(Guid id);
}
