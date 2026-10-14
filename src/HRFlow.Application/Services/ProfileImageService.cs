using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Services;

/// <summary>Versioned own images have a separate lifecycle; only HR may read another employee's current image.</summary>
public sealed class ProfileImageService(IProfileImageContext db, CurrentAccountAuthorization authorization,
    IEmployeeRoleLookupService roles, IEmployeeManagementTransaction writes, ILeaveReportingReadTransaction reads,
    IProfileImageStorage storage, IDocumentScanner scanner)
{
    private static readonly string[] All = [EmployeeRoles.Employee, EmployeeRoles.Manager, EmployeeRoles.HrAdministrator];
    private static ProfileImageDto Project(ProfileImage? image) => new(image?.Version ?? Guid.Empty, image?.Size > 0);
    private Task<ProfileImage?> Current(Guid owner, CancellationToken token) => db.ProfileImages.SingleOrDefaultAsync(i => i.OwnerId == owner && i.Status == ProfileImageStatus.Current, token);
    private static NotFoundException Missing() => new("Profile image was not found or is unavailable to your account.");

    /// <summary>Initials have the explicit empty version; image versions are independent of profile and employment edits.</summary>
    public Task<ProfileImageDto> GetAsync(Guid identity, CancellationToken token) => reads.ExecuteAsync(async ct =>
    { var actor = await authorization.RequireIdentityAsync(identity, All, ct); return Project(await Current(actor.Id, ct)); }, token);

    /// <summary>Registers durable compensation before processing, then rechecks the original generation before switching.</summary>
    public async Task<ProfileImageDto> ReplaceAsync(Guid identity, Guid expectedVersion, Stream input, string extension, string mime, ImageCrop crop, CancellationToken token)
    {
        ProfileImage pending = null!;
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, All, ct);
            if (Project(await Current(actor.Id, ct)).Version != expectedVersion) throw new WriteConflictException("Profile image changed. Reload before replacing it.");
            if (await db.ProfileImages.CountAsync(i => i.OwnerId == actor.Id && i.Status == ProfileImageStatus.Processing, ct) >= 2)
                throw new WriteConflictException("An image is already processing. Wait or reload before retrying.");
            pending = new ProfileImage { OwnerId = actor.Id }; db.ProfileImages.Add(pending);
        }, token);
        try
        {
            var output = await storage.ProcessAsync(input, extension, mime, crop, token);
            StoredDocument facts;
            try
            {
                using var scan = new MemoryStream(output, false);
                if (await scanner.ScanAsync(scan, token) != SupportingDocumentStatus.Clean)
                    throw new WriteConflictException("Image scanning is unavailable or rejected the image. Your previous image is unchanged; retry explicitly.");
                facts = await storage.PublishAsync(pending.Id, output, token);
            }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(output); }
            ProfileImageDto result = null!;
            await writes.ExecuteAsync(async ct =>
            {
                var actor = await authorization.RequireIdentityAsync(identity, All, ct);
                var old = await Current(actor.Id, ct);
                if (Project(old).Version != expectedVersion) throw new WriteConflictException("Profile image changed. Reload before replacing it.");
                var row = await db.ProfileImages.SingleAsync(i => i.Id == pending.Id && i.OwnerId == actor.Id, ct);
                if (row.Status != ProfileImageStatus.Processing) throw new WriteConflictException("Image processing expired. Retry explicitly.");
                if (old != null) { old.Status = ProfileImageStatus.Removed; old.UpdatedUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
                row.Status = ProfileImageStatus.Current; row.Size = facts.Size; row.Checksum = facts.Checksum;
                row.UpdatedUtc = DateTime.UtcNow; result = Project(row);
            }, token);
            return result;
        }
        catch
        {
            // Permanent publication tombstone wins against delayed publication; metadata survives failed compensation.
            var removed = false;
            try
            {
                await writes.ExecuteAsync(async ct =>
                {
                    var row = await db.ProfileImages.SingleAsync(i => i.Id == pending.Id, ct);
                    if (row.Status == ProfileImageStatus.Processing) { row.Status = ProfileImageStatus.Removed; row.UpdatedUtc = DateTime.UtcNow; }
                    removed = row.Status == ProfileImageStatus.Removed;
                }, CancellationToken.None);
            }
            catch { /* Worker retries interrupted processing without erasing the committed image. */ }
            if (removed) { try { storage.Remove(pending.Id); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
            throw;
        }
    }

    /// <summary>Removal rotates the absence generation using a retained empty current row to prevent ABA replacement.</summary>
    public async Task RemoveAsync(Guid identity, Guid expectedVersion, CancellationToken token)
    {
        var removed = new List<Guid>();
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, All, ct);
            var old = await Current(actor.Id, ct);
            if (Project(old).Version != expectedVersion) throw new WriteConflictException("Profile image changed. Reload before removing it.");
            if (old != null) { old.Status = ProfileImageStatus.Removed; old.UpdatedUtc = DateTime.UtcNow; removed.Add(old.Id); }
            foreach (var pending in await db.ProfileImages.Where(i => i.OwnerId == actor.Id && i.Status == ProfileImageStatus.Processing).ToListAsync(ct))
            { pending.Status = ProfileImageStatus.Removed; pending.UpdatedUtc = DateTime.UtcNow; removed.Add(pending.Id); }
            await db.SaveChangesAsync(ct);
            db.ProfileImages.Add(new ProfileImage { OwnerId = actor.Id, Status = ProfileImageStatus.Current });
        }, token);
        // Small file operations share cross-process publication locks outside SQLite; failed cleanup remains durable.
        foreach (var id in removed) { try { storage.Remove(id); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
    }

    /// <summary>HR directory visibility is additive; Manager alone never grants arbitrary image access.</summary>
    public async Task<Stream> DownloadAsync(Guid identity, Guid? employeeId, CancellationToken token)
    {
        var snapshot = await reads.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, All, ct);
            var owner = employeeId ?? actor.Id;
            if (owner != actor.Id && !(await roles.GetRolesByIdentityUserIdAsync(actor.IdentityUserId, ct)).Contains(EmployeeRoles.HrAdministrator)) throw Missing();
            var image = await Current(owner, ct);
            return image is { Size: > 0 } ? image : throw Missing();
        }, token);
        try { return await storage.OpenVerifiedAsync(snapshot.Id, snapshot.Size, snapshot.Checksum, token); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { throw Missing(); }
    }
}
