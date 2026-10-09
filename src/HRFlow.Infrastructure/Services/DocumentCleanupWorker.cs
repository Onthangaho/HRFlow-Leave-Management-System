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
                    && !db.DocumentAccessEntries.Any(a => a.DocumentId == d.Id && a.Action == "BlobCleanup")
                    && (d.Status == SupportingDocumentStatus.Removed || d.UpdatedUtc < cutoff || (d.Status == SupportingDocumentStatus.Receiving || d.Status == SupportingDocumentStatus.Scanning) && d.UpdatedUtc < interrupted))
                    .OrderBy(d => d.UpdatedUtc).Take(20).Select(d => d.Id).ToArrayAsync(token);
                foreach (var id in ids)
                {
                    var remove = false;
                    await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                    {
                        var d = await db.SupportingDocuments.SingleAsync(d => d.Id == id, ct);
                        if (d.RequestId != null) return;
                        if (d.Status != SupportingDocumentStatus.Removed && d.UpdatedUtc >= cutoff && (!(d.Status is SupportingDocumentStatus.Receiving or SupportingDocumentStatus.Scanning) || d.UpdatedUtc >= interrupted)) return;
                        if (d.Status != SupportingDocumentStatus.Removed) { d.Status = SupportingDocumentStatus.Removed; d.Version = Guid.NewGuid(); d.UpdatedUtc = DateTime.UtcNow; d.RemovedUtc = DateTime.UtcNow; db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, ActorId = null, Action = "DraftExpiry" }); }
                        remove = true;
                    }, token);
                    if (remove)
                    {
                        scope.ServiceProvider.GetRequiredService<IPrivateDocumentStorage>().Remove(id);
                        await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                        {
                            if (!await db.DocumentAccessEntries.AnyAsync(a => a.DocumentId == id && a.Action == "BlobCleanup", ct))
                                db.DocumentAccessEntries.Add(new DocumentAccessEntry { DocumentId = id, Action = "BlobCleanup" });
                        }, token);
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception exception) { logger.LogWarning("Document cleanup deferred. Category: {Category}", exception.GetType().Name); }
            await Task.Delay(TimeSpan.FromMinutes(5), token);
        }
    }
}
