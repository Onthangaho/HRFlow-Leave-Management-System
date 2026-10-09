using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Services;

/// <summary>Own draft lifecycle and current document scope share the existing account and SQLite safeguards.</summary>
public sealed class SupportingDocumentService(IApplicationDocumentContext db, CurrentAccountAuthorization authorization,
    IEmployeeRoleLookupService roles, IEmployeeManagementTransaction writes, ILeaveReportingReadTransaction reads,
    IPrivateDocumentStorage storage, IDocumentScanner scanner)
{
    private static readonly string[] Personal = [EmployeeRoles.Employee, EmployeeRoles.Manager];
    private static readonly string[] All = [EmployeeRoles.Employee, EmployeeRoles.Manager, EmployeeRoles.HrAdministrator];

    /// <summary>Registers ownership before transferring bytes; duplicate upload keys never overwrite an existing object.</summary>
    public async Task<DocumentDto> UploadAsync(Guid identity, Guid key, string classification, Stream input, string extension, string mime, CancellationToken token)
    {
        if (key == Guid.Empty || classification is not (SupportingDocumentClass.Medical or SupportingDocumentClass.Ordinary)) throw new ArgumentException("Choose Medical or Ordinary evidence and a valid upload key.");
        SupportingDocument document = null!; var created = false;
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, Personal, ct);
            var existing = await db.SupportingDocuments.SingleOrDefaultAsync(d => d.OwnerId == actor.Id && d.UploadKey == key, ct);
            if (existing != null)
            {
                if (existing.Class != classification) throw new WriteConflictException("An upload key cannot change evidence classification.");
                document = existing; return;
            }
            if (await db.SupportingDocuments.CountAsync(d => d.OwnerId == actor.Id && d.RequestId == null && d.Status != SupportingDocumentStatus.Removed, ct) >= SupportingDocumentLimits.MaxOwnerDrafts)
                throw new WriteConflictException("Remove unused draft uploads before adding more.");
            document = new SupportingDocument { OwnerId = actor.Id, UploadKey = key, Class = classification };
            db.SupportingDocuments.Add(document); created = true;
        }, token);
        if (!created) return Project(document, true);
        try
        {
            var stored = await storage.StageAsync(document.Id, input, extension, mime, token);
            await writes.ExecuteAsync(async ct =>
            {
                await authorization.RequireIdentityAsync(identity, Personal, ct);
                document = await Owned(document.Id, document.OwnerId, ct);
                if (document.Status != SupportingDocumentStatus.Receiving) throw new WriteConflictException("Upload draft changed while transferring.");
                document.Size = stored.Size; document.Checksum = stored.Checksum; document.MediaType = stored.MediaType;
                Change(document, SupportingDocumentStatus.Quarantined);
            }, token);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Durable Receiving metadata lets cleanup compensate even when this process or request dies.
            throw new ArgumentException("Upload could not be completed. Remove this draft and retry with a new upload key.");
        }
        return await ScanAsync(identity, document.Id, token);
    }

    /// <summary>Scanner outages retain inaccessible quarantine; retries scan the same immutable bytes.</summary>
    public async Task<DocumentDto> ScanAsync(Guid identity, Guid id, CancellationToken token)
    {
        SupportingDocument document = null!;
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, Personal, ct);
            document = await Owned(id, actor.Id, ct);
            if (document.Status is not (SupportingDocumentStatus.Quarantined or SupportingDocumentStatus.ScanUnavailable)) throw new WriteConflictException("This document cannot be rescanned. Reload its status.");
            Change(document, SupportingDocumentStatus.Scanning);
        }, token);
        string verdict;
        try
        {
            using var stream = await storage.OpenVerifiedAsync(id, false, document.Size, document.Checksum, token);
            verdict = await scanner.ScanAsync(stream, token);
            if (verdict == SupportingDocumentStatus.Clean) storage.Promote(id, stream);
        }
        catch { verdict = SupportingDocumentStatus.ScanUnavailable; }
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, Personal, ct);
            document = await Owned(id, actor.Id, ct);
            if (document.Status != SupportingDocumentStatus.Scanning) throw new WriteConflictException("The document lifecycle changed during scanning.");
            Change(document, verdict);
            if (verdict is SupportingDocumentStatus.Clean or SupportingDocumentStatus.Rejected) document.ScannedUtc = DateTime.UtcNow;
        }, token);
        return Project(document, true);
    }

    /// <summary>Binds only owned clean uploads inside submission's existing reservation, without another transition audit.</summary>
    public async Task BindAsync(Guid employee, Guid request, IReadOnlyCollection<Guid> ids, IReadOnlyDictionary<Guid, Guid> verified, CancellationToken ct)
    {
        if (ids.Count > SupportingDocumentLimits.MaxRequestDocuments || ids.Distinct().Count() != ids.Count) throw new FluentValidation.ValidationException("Choose at most five distinct documents.");
        foreach (var id in ids)
        {
            var document = await Owned(id, employee, ct);
            if (document.Status != SupportingDocumentStatus.Clean || document.RequestId != null) throw new WriteConflictException("Only clean, unbound owned documents may be attached.");
            if (!verified.TryGetValue(id, out var version) || version != document.Version)
                throw new WriteConflictException("Document changed. Refresh evidence before submitting.");
            document.RequestId = request; document.Version = Guid.NewGuid(); document.UpdatedUtc = DateTime.UtcNow;
            document.BoundUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Checks bounded content outside the writer reservation; binding rechecks the original metadata version under protection.</summary>
    public async Task<Dictionary<Guid, Guid>> VerifyBindingAsync(Guid employee, IReadOnlyCollection<Guid> ids, CancellationToken token)
    {
        if (ids.Count > SupportingDocumentLimits.MaxRequestDocuments || ids.Distinct().Count() != ids.Count)
            throw new FluentValidation.ValidationException("Choose at most five distinct documents.");
        var documents = await reads.ExecuteAsync(async ct =>
        {
            await authorization.RequireEmployeeAsync(employee, Personal, ct);
            var result = new List<SupportingDocument>();
            foreach (var id in ids)
            {
                var d = await db.SupportingDocuments.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id && d.OwnerId == employee, ct) ?? throw Missing();
                if (d.Status != SupportingDocumentStatus.Clean || d.RequestId != null) throw new WriteConflictException("Only clean, unbound owned documents may be attached.");
                result.Add(d);
            }
            return result;
        }, token);
        foreach (var d in documents)
        {
            try { using var content = await storage.OpenVerifiedAsync(d.Id, true, d.Size, d.Checksum, token); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { throw new WriteConflictException("Document content is unavailable. Refresh evidence before submitting."); }
        }
        return documents.ToDictionary(d => d.Id, d => d.Version);
    }

    /// <summary>Read scope and medical redaction use one current authorization snapshot.</summary>
    public Task<List<DocumentDto>> ListAsync(Guid identity, Guid? request, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        var actor = await authorization.RequireIdentityAsync(identity, All, ct);
        if (request == null)
            return (await db.SupportingDocuments.AsNoTracking().Where(d => d.OwnerId == actor.Id && d.RequestId == null && d.Status != SupportingDocumentStatus.Removed).OrderBy(d => d.CreatedUtc).ToListAsync(ct)).Select(d => Project(d, true)).ToList();
        var (owner, hr) = await RequestScope(actor, request.Value, ct);
        return (await db.SupportingDocuments.AsNoTracking().Where(d => d.RequestId == request).OrderBy(d => d.CreatedUtc).ToListAsync(ct))
            .Select(d => Project(d, d.Class == SupportingDocumentClass.Ordinary || hr || owner.Id == actor.Id)).ToList();
    }, token);

    /// <summary>Rechecks credentials/scope and commits an access record before bytes leave database protection.</summary>
    public async Task<DocumentDownload> DownloadAsync(Guid identity, Guid id, CancellationToken token)
    {
        var snapshot = await reads.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, All, ct);
            var d = await db.SupportingDocuments.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id, ct) ?? throw Missing();
            var allowed = actor.Id == d.OwnerId;
            if (d.RequestId != null) { var (_, hr) = await RequestScope(actor, d.RequestId.Value, ct); allowed |= hr || d.Class == SupportingDocumentClass.Ordinary; }
            if (!allowed || d.Status != SupportingDocumentStatus.Clean) throw Missing();
            return d;
        }, token);
        Stream? verifiedContent = null;
        try { verifiedContent = await storage.OpenVerifiedAsync(id, true, snapshot.Size, snapshot.Checksum, token); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { /* Record unavailable content only after another authoritative scope check. */ }
        DocumentDownload? result = null;
        try { await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, All, ct);
            var d = await db.SupportingDocuments.SingleOrDefaultAsync(d => d.Id == id, ct) ?? throw Missing();
            var allowed = actor.Id == d.OwnerId;
            if (d.RequestId != null) { var (_, hr) = await RequestScope(actor, d.RequestId.Value, ct); allowed |= hr || d.Class == SupportingDocumentClass.Ordinary; }
            if (!allowed || d.Status != SupportingDocumentStatus.Clean) throw Missing();
            if (d.Version != snapshot.Version) throw Missing();
            if (verifiedContent == null)
            {
                Change(d, SupportingDocumentStatus.Missing); db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, ActorId = actor.Id, Action = "ContentUnavailable" });
                return;
            }
            db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, ActorId = actor.Id, Action = "Download" });
            result = new(verifiedContent, d.MediaType);
        }, token); }
        catch { verifiedContent?.Dispose(); throw; }
        return result ?? throw new NotFoundException("Document content is unavailable.");
    }

    /// <summary>Only unbound owned drafts can be removed; metadata survives failed blob deletion for reconciliation.</summary>
    public async Task RemoveAsync(Guid identity, Guid id, Guid version, CancellationToken token)
    {
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, Personal, ct); var d = await Owned(id, actor.Id, ct);
            if (d.Version != version) throw new WriteConflictException("Document changed. Reload before removing it.");
            if (d.RequestId != null) throw new WriteConflictException("Bound evidence cannot be removed without an approved retention policy.");
            if (d.Status == SupportingDocumentStatus.Removed) return;
            Change(d, SupportingDocumentStatus.Removed); d.RemovedUtc = DateTime.UtcNow; db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, ActorId = actor.Id, Action = "Remove" });
        }, token);
        try { storage.Remove(id); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { /* Durable Removed state is retried by draft cleanup; content stays inaccessible. */ }
    }

    private async Task<(Employee Owner, bool Hr)> RequestScope(Employee actor, Guid id, CancellationToken ct)
    {
        var capabilities = await roles.GetRolesByIdentityUserIdAsync(actor.IdentityUserId, ct);
        var hr = capabilities.Contains(EmployeeRoles.HrAdministrator);
        var request = await db.LeaveRequests.AsNoTracking().Include(r => r.Employee).SingleOrDefaultAsync(r => r.Id == id, ct) ?? throw Missing();
        if (!hr && request.EmployeeId != actor.Id && !(capabilities.Contains(EmployeeRoles.Manager) && request.Employee.ManagerId == actor.Id && request.Employee.DepartmentId == actor.DepartmentId)) throw Missing();
        return (request.Employee, hr);
    }
    private async Task<SupportingDocument> Owned(Guid id, Guid owner, CancellationToken ct) => await db.SupportingDocuments.SingleOrDefaultAsync(d => d.Id == id && d.OwnerId == owner, ct) ?? throw Missing();
    private static NotFoundException Missing() => new("Document was not found or is not available to your account.");
    private static DocumentDto Project(SupportingDocument d, bool content) => new(d.Id, d.Class, d.Status, d.Version, content ? d.Size : null, content ? d.MediaType : null, content && d.Status == SupportingDocumentStatus.Clean);
    private static void Change(SupportingDocument d, string status) { d.Status = status; d.Version = Guid.NewGuid(); d.UpdatedUtc = DateTime.UtcNow; }
}
