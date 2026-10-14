namespace HRFlow.Application.Interfaces;
/// <summary>Actor-owned unfinished reviews expose only recovery links/counts, never uploaded personal rows.</summary>
public sealed record ImportAttentionItem(Guid Id, DateTime ExpiresAtUtc, int ReadyRows, int OtherUncreatedRows);
/// <summary>Counts all live reviews, with a bounded latest-first list for actionable links.</summary>
public sealed record ImportAttentionDto(int OpenBatches, int UncreatedRows, int PreviewLimit, IReadOnlyList<ImportAttentionItem> Batches);
/// <summary>Reads onboarding storage in the caller's existing authorized deferred snapshot.</summary>
public interface IHrImportAttentionReader
{
    /// <summary>Other HR users' reviews are not interchangeable authorizations; only the initiating actor's live unfinished rows count.</summary>
    Task<ImportAttentionDto> ReadAsync(Guid actorIdentityId, DateTime asOfUtc, CancellationToken token);
}
