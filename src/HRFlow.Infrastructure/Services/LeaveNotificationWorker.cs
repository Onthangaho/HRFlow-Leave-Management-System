using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services;

/// <summary>Recovers committed work across restarts; each short transaction atomically delivers and acknowledges one event.</summary>
public sealed class LeaveNotificationWorker(IServiceScopeFactory scopes, ILogger<LeaveNotificationWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Guid[] ids;
                using (var scope = scopes.CreateScope())
                    ids = await scope.ServiceProvider.GetRequiredService<HRFlowDbContext>().LeaveNotificationEvents.AsNoTracking()
                        .Where(e => e.DeliveredAtUtc == null && (e.RetryAfterUtc == null || e.RetryAfterUtc <= DateTime.UtcNow)).OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.Id)
                        .Take(BatchSize).Select(e => e.Id).ToArrayAsync(stoppingToken);
                foreach (var id in ids)
                {
                    try
                    {
                        using var scope = scopes.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
                        await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                        {
                            var source = await db.LeaveNotificationEvents.SingleAsync(e => e.Id == id, ct);
                            if (source.DeliveredAtUtc != null) return;
                            if (!await db.LeaveNotifications.AnyAsync(n => n.EventId == id && n.RecipientId == source.RecipientId, ct))
                                db.LeaveNotifications.Add(new LeaveNotification { EventId = id, RecipientId = source.RecipientId, CreatedAtUtc = source.CreatedAtUtc });
                            source.DeliveredAtUtc = DateTime.UtcNow;
                        }, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch (Exception exception)
                    {
                        // Never log exception messages: a provider error can include data values.
                        try
                        {
                            using var retryScope = scopes.CreateScope();
                            var retryDb = retryScope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
                            await retryScope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                            {
                                var failed = await retryDb.LeaveNotificationEvents.SingleAsync(e => e.Id == id, ct);
                                if (failed.DeliveredAtUtc is null) failed.RetryAfterUtc = DateTime.UtcNow.Add(FailureRetryDelay);
                            }, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                        catch (Exception retryFailure)
                        {
                            logger.LogWarning("Notification retry scheduling deferred. EventId: {EventId}; Category: {Category}", id, retryFailure.GetType().Name);
                        }
                        logger.LogWarning("Notification delivery deferred. EventId: {EventId}; Category: {Category}", id, exception.GetType().Name);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogWarning("Notification batch unavailable. Category: {Category}", exception.GetType().Name);
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
