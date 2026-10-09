using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services;

/// <summary>Durable draft-only compensation; bound evidence is never automatically purged.</summary>
public sealed class DocumentCleanupWorker(IServiceScopeFactory scopes, ILogger<DocumentCleanupWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private int removedOffset;
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-7); var interrupted = DateTime.UtcNow.AddHours(-1);
                var ids = await db.SupportingDocuments.AsNoTracking().Where(d => d.RequestId == null
                    && d.Status != SupportingDocumentStatus.Removed
                    && (d.UpdatedUtc < cutoff || (d.Status == SupportingDocumentStatus.Receiving || d.Status == SupportingDocumentStatus.Scanning) && d.UpdatedUtc < interrupted))
                    .OrderBy(d => d.UpdatedUtc).Take(BatchSize).Select(d => d.Id).ToArrayAsync(token);
                foreach (var id in ids)
                {
                    await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                    {
                        var d = await db.SupportingDocuments.SingleAsync(d => d.Id == id, ct);
                        if (d.RequestId != null) return;
                        if (d.Status != SupportingDocumentStatus.Removed && d.UpdatedUtc >= cutoff && (!(d.Status is SupportingDocumentStatus.Receiving or SupportingDocumentStatus.Scanning) || d.UpdatedUtc >= interrupted)) return;
                        if (d.Status != SupportingDocumentStatus.Removed) { d.Status = SupportingDocumentStatus.Removed; d.Version = Guid.NewGuid(); d.UpdatedUtc = DateTime.UtcNow; d.RemovedUtc = DateTime.UtcNow; db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, ActorId = null, Action = "DraftExpiry" }); }
                    }, token);
                }
                // Cleanup acknowledgements record an outcome, not a permanent exclusion. Round-robin pages
                // eventually revisit every removed row, including pre-upgrade leftovers, without starving new expiry.
                var removed = await db.SupportingDocuments.AsNoTracking()
                    .Where(d => d.RequestId == null && d.Status == SupportingDocumentStatus.Removed)
                    .OrderBy(d => d.Id).Skip(removedOffset).Take(BatchSize).Select(d => d.Id).ToArrayAsync(token);
                removedOffset = removed.Length < BatchSize ? 0 : removedOffset + removed.Length;
                foreach (var id in removed)
                {
                    try
                    {
                        scope.ServiceProvider.GetRequiredService<IPrivateDocumentStorage>().Remove(id);
                        await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                        {
                            // Removed is terminal in this workflow. Recheck so a malformed bound row cannot gain a cleanup acknowledgement.
                            if (!await db.SupportingDocuments.AnyAsync(d => d.Id == id && d.RequestId == null && d.Status == SupportingDocumentStatus.Removed, ct)) return;
                            if (!await db.DocumentAccessEntries.AnyAsync(a => a.DocumentId == id && a.Action == "BlobCleanup", ct))
                                db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, Action = "BlobCleanup" });
                        }, token);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    { logger.LogWarning("Document cleanup deferred. DocumentId: {DocumentId}; Category: {Category}", id, exception.GetType().Name); }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception exception) { logger.LogWarning("Document cleanup deferred. Category: {Category}", exception.GetType().Name); }
            await Task.Delay(TimeSpan.FromMinutes(5), token);
        }
    }
}
