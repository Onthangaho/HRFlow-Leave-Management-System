namespace HRFlow.Application.Interfaces;
/// <summary>Minimal review contract; no passwords or invitation tokens leave the creation transaction.</summary>
public sealed record ImportRow(int Row, Dictionary<string,string> Fields, string State, string[] Errors,
 Guid? DepartmentId = null, Guid? ManagerId = null, Guid? ManagerVersion = null,
 Guid? TemplateId = null, Guid? TemplateVersion = null, Guid? EmployeeId = null, string? DeliveryState = null);
/// <summary>Frozen actor-owned review generation; expiry prohibits further creation, not reading outcomes.</summary>
public sealed record ImportPreview(Guid Id, Guid Version, DateTime ExpiresAtUtc, bool Cancelled, IReadOnlyList<ImportRow> Rows);
/// <summary>Versioned template input, with Monday-Sunday whole working minutes.</summary>
public sealed record ImportTemplateInput(string Code, string Name, int[] Minutes, Guid? ExpectedVersion);
/// <summary>Snapshot response suitable for unambiguous code selection.</summary>
public sealed record ImportTemplateDto(Guid Id, string Code, string Name, int[] Minutes, Guid Version);
/// <summary>Separates HTTP transport from CSV parsing, actor-scoped review and row transactions.</summary>
public interface ICsvOnboardingService
{
 /// <summary>Fully validates encoding/structure before exposing only headers for mapping.</summary>
 Task<string[]> InspectAsync(Guid actor, byte[] bytes, CancellationToken token);
 /// <summary>Normalizes mapped rows and persists a review without creating accounts.</summary>
 Task<ImportPreview> PreviewAsync(Guid actor, byte[] bytes, Dictionary<string,string> mapping, CancellationToken token);
 /// <summary>Revalidates failed/ready rows explicitly while retaining committed outcomes.</summary>
 Task<ImportPreview> RevalidateAsync(Guid actor, Guid id, Guid version, CancellationToken token);
 /// <summary>Creates selected Ready rows once, each in its own protected transaction.</summary>
 Task<ImportPreview> ConfirmAsync(Guid actor, Guid id, Guid version, string key, int[] rows, CancellationToken token);
 /// <summary>Stops remaining work without undoing committed employees.</summary>
 Task<ImportPreview> CancelAsync(Guid actor, Guid id, CancellationToken token);
 /// <summary>Reads only the actor's review/outcomes under a consistent authorization snapshot.</summary>
 Task<ImportPreview> GetAsync(Guid actor, Guid id, CancellationToken token);
 /// <summary>Returns minimal, formula-safe result CSV without row personal data.</summary>
 Task<byte[]> ResultsAsync(Guid actor, Guid id, CancellationToken token);
 /// <summary>Reads HR-only versioned fixed-pattern lookup.</summary>
 Task<IReadOnlyList<ImportTemplateDto>> TemplatesAsync(Guid actor, CancellationToken token);
 /// <summary>Creates or replaces an explicit template generation; imported revisions remain unchanged.</summary>
 Task<ImportTemplateDto> SaveTemplateAsync(Guid actor, ImportTemplateInput input, CancellationToken token);
}
