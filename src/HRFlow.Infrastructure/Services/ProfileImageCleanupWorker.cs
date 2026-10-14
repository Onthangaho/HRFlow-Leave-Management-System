using HRFlow.Domain.Entities;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services;

/// <summary>Bounded restart compensation revisits removed images; tombstones permanently prohibit late publication.</summary>
public sealed class ProfileImageCleanupWorker(IServiceScopeFactory scopes, ILogger<ProfileImageCleanupWorker> logger) : BackgroundService
{
    private int offset;
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
                await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct =>
                {
                    var cutoff = DateTime.UtcNow.AddHours(-1);
                    var expired = await db.ProfileImages.Where(i => i.Status == ProfileImageStatus.Processing && i.UpdatedUtc < cutoff).OrderBy(i => i.UpdatedUtc).Take(20).ToListAsync(ct);
                    foreach (var image in expired) { image.Status = ProfileImageStatus.Removed; image.UpdatedUtc = DateTime.UtcNow; }
                }, token);
                var removed = await db.ProfileImages.AsNoTracking().Where(i => i.Status == ProfileImageStatus.Removed).OrderBy(i => i.Id).Skip(offset).Take(20).Select(i => i.Id).ToArrayAsync(token);
                offset = removed.Length < 20 ? 0 : offset + removed.Length;
                foreach (var id in removed)
                {
                    try { scope.ServiceProvider.GetRequiredService<IProfileImageStorage>().Remove(id); }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                    { logger.LogWarning("Image cleanup deferred. ImageId: {ImageId}; Category: {Category}", id, error.GetType().Name); }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception error) { logger.LogWarning("Image cleanup deferred. Category: {Category}", error.GetType().Name); }
            await Task.Delay(TimeSpan.FromMinutes(1), token);
        }
    }
}
