using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Services;

public sealed partial class SupportingDocumentService
{
    /// <summary>Authorised detail history exposes later additions separately from submitted requirements and transitions.</summary>
    public Task<RequestEvidenceDto> AdditionsAsync(Guid identity, Guid requestId, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        var actor = await authorization.RequireIdentityAsync(identity, All, ct);
        var (owner, _) = await RequestScope(actor, requestId, ct);
        var request = await db.LeaveRequests.AsNoTracking().SingleAsync(r => r.Id == requestId, ct);
        var capabilities = await roles.GetRolesByIdentityUserIdAsync(actor.IdentityUserId, ct);
        var reason = actor.Id != owner.Id || !capabilities.Any(Personal.Contains)
            ? "Only the current request owner with personal leave access may add evidence." : AdditionRestriction(request);
        var entries = await db.EvidenceAdditions.AsNoTracking().Where(e => e.RequestId == requestId)
            .OrderBy(e => e.TimestampUtc).ThenBy(e => e.Id).ToListAsync(ct);
        return new RequestEvidenceDto(reason == null, reason ?? "Optional later evidence does not change the requirements captured at submission.",
            request.SubmissionRequirements?.EvidenceClass, await db.SupportingDocuments.CountAsync(d => d.RequestId == requestId, ct), entries.Select(AdditionDto).ToList());
    }, token);

    /// <summary>Verifies bounded bytes outside writes, then atomically binds, traces and records recovery under the writer reservation.</summary>
    public async Task<EvidenceAdditionDto> AddAsync(Guid identity, Guid requestId, AddEvidenceRequest input, CancellationToken token)
    {
        if (input.Key == Guid.Empty || input.Documents == null || input.Documents.Count is < 1 or > SupportingDocumentLimits.MaxRequestDocuments
            || input.Documents.Any(d => d == null || d.Id == Guid.Empty || d.Version == Guid.Empty) || input.Documents.Select(d => d.Id).Distinct().Count() != input.Documents.Count)
            throw new FluentValidation.ValidationException("Choose one to five distinct clean drafts with their original versions and a valid addition key.");
        var payload = string.Join(";", input.Documents.OrderBy(d => d.Id).Select(d => $"{d.Id:D}:{d.Version:D}"));
        var initial = await reads.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, Personal, ct);
            var request = await OwnRequest(actor.Id, requestId, ct);
            var previous = await db.EvidenceAdditions.AsNoTracking().SingleOrDefaultAsync(e => e.ActorId == actor.Id && e.RequestId == requestId && e.Key == input.Key, ct);
            if (previous != null) CheckPayload(previous, payload);
            else if (AdditionRestriction(request) is { } reason) throw new WriteConflictException(reason);
            return (actor.Id, Previous: previous);
        }, token);
        // Recovery still enters the writer gate below, rechecking current credentials and ownership.
        var verified = new Dictionary<Guid, Guid>();
        WriteConflictException? verificationFailure = null;
        if (initial.Previous == null)
        {
            try
            {
                verified = await VerifyBindingAsync(initial.Id, input.Documents.Select(d => d.Id).ToArray(), token);
                if (input.Documents.Any(d => !verified.TryGetValue(d.Id, out var version) || version != d.Version))
                    verificationFailure = new WriteConflictException("Evidence changed. Refresh drafts and review before trying again.");
            }
            catch (WriteConflictException failure) { verificationFailure = failure; }
        }
        EvidenceAdditionDto result = null!;
        await writes.ExecuteAsync(async ct =>
        {
            var actor = await authorization.RequireIdentityAsync(identity, Personal, ct);
            var request = await OwnRequest(actor.Id, requestId, ct);
            var previous = await db.EvidenceAdditions.SingleOrDefaultAsync(e => e.ActorId == actor.Id && e.RequestId == requestId && e.Key == input.Key, ct);
            if (previous != null) { CheckPayload(previous, payload); result = AdditionDto(previous); return; }
            if (verificationFailure != null) throw verificationFailure;
            if (AdditionRestriction(request) is { } reason) throw new WriteConflictException(reason);
            var count = await db.SupportingDocuments.CountAsync(d => d.RequestId == requestId, ct);
            if (count + input.Documents.Count > SupportingDocumentLimits.MaxRequestDocuments)
                throw new WriteConflictException("A request may have at most five documents in total. Refresh and explicitly review your selection.");
            await BindAsync(actor.Id, requestId, input.Documents.Select(d => d.Id).ToArray(), verified, ct, request.SubmissionRequirements!.EvidenceClass);
            var entry = new EvidenceAddition { ActorId = actor.Id, RequestId = requestId, Key = input.Key, DocumentIds = payload };
            db.EvidenceAdditions.Add(entry);
            foreach (var document in input.Documents)
                db.DocumentAccessEntries.Add(new DocumentAccessEntry { ActorId = actor.Id, DocumentId = document.Id, Action = "SupplementalBind", TimestampUtc = entry.TimestampUtc });
            result = AdditionDto(entry);
        }, token);
        return result;
    }

    private async Task<LeaveRequest> OwnRequest(Guid owner, Guid requestId, CancellationToken token) =>
        await db.LeaveRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == requestId && r.EmployeeId == owner, token) ?? throw Missing();
    private static string? AdditionRestriction(LeaveRequest request)
    {
        if (request.Status != LeaveRequestStatus.Pending) return "Only Pending requests can accept new evidence. Recorded evidence and history remain available.";
        var snapshot = request.SubmissionRequirements;
        if (snapshot == null) return "Unknown legacy requirements cannot be inferred. This request cannot accept later additions.";
        if (snapshot.EvidenceMode is not (RequirementModes.Optional or RequirementModes.Required)) return "The captured requirements do not request evidence. Later type changes cannot create a new demand.";
        if (snapshot.EvidenceClass is not (SupportingDocumentClass.Ordinary or SupportingDocumentClass.Medical)
            || snapshot.EvidenceMode == RequirementModes.Required && snapshot.EvidenceClass == SupportingDocumentClass.Medical)
            return "This captured evidence workflow is unsupported. Required Medical submission is not enabled.";
        return null;
    }
    private static void CheckPayload(EvidenceAddition entry, string payload)
    { if (entry.DocumentIds != payload) throw new WriteConflictException("An addition key cannot be reused with a changed selection or document versions."); }
    private static EvidenceAdditionDto AdditionDto(EvidenceAddition entry) => new(entry.Id, entry.ActorId, entry.RequestId,
        DateTime.SpecifyKind(entry.TimestampUtc, DateTimeKind.Utc), entry.DocumentIds.Split(';').Select(s => Guid.Parse(s.Split(':')[0])).ToArray());
}
