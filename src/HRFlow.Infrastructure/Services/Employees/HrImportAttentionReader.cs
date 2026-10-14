using System.Text.Json;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HRFlow.Infrastructure.Services.Employees;
/// <summary>Decodes persisted outcomes privately; starts no transaction and returns no source fields or delivery secrets.</summary>
public sealed class HrImportAttentionReader(HRFlowDbContext context) : IHrImportAttentionReader
{
    private const int PreviewLimit = 20;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    /// <inheritdoc />
    public async Task<ImportAttentionDto> ReadAsync(Guid actorIdentityId, DateTime asOfUtc, CancellationToken token)
    {
        var batches = await context.OnboardingBatches.AsNoTracking().Where(b => b.ActorId == actorIdentityId
            && !b.Cancelled && b.ExpiresAtUtc > asOfUtc).OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.Id)
            .Select(b => new { b.Id, b.ExpiresAtUtc, b.RowsJson }).ToListAsync(token);
        var items = batches.Select(b => {
            var rows = JsonSerializer.Deserialize<List<ImportRow>>(b.RowsJson, JsonOptions) ?? throw new InvalidOperationException("Onboarding review rows are unavailable.");
            return new ImportAttentionItem(b.Id, DateTime.SpecifyKind(b.ExpiresAtUtc, DateTimeKind.Utc),
                rows.Count(r => r.State == "Ready"), rows.Count(r => r.State != "Created" && r.State != "Ready"));
        }).Where(b => b.ReadyRows + b.OtherUncreatedRows > 0).ToList();
        return new(items.Count, items.Sum(b => b.ReadyRows + b.OtherUncreatedRows), PreviewLimit, items.Take(PreviewLimit).ToArray());
    }
}
