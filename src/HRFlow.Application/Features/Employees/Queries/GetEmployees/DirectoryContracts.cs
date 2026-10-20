namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

/// <summary>Filters precede paging; search is literal ASCII-case-insensitive substring matching.</summary>
public sealed record EmployeeDirectoryFilter
{
    public string? Search { get; init; }
    public string Status { get; init; } = "All";
    public Guid? DepartmentId { get; init; }
    public Guid? ReportsTo { get; init; }
    public string Attention { get; init; } = "all";
    public string Sort { get; init; } = "name";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>Count and items share one authorised snapshot, including out-of-range empty pages.</summary>
public sealed record EmployeeDirectoryPage(IReadOnlyList<GetEmployeeDto> Items, int TotalCount, int Page, int PageSize, DateTimeOffset AsOfUtc);
/// <summary>Minimal manager choices are independent of the directory page.</summary>
public sealed record ManagerChoice(Guid Id, string FullName);
/// <summary>The assigned snapshot may be ineligible today; it is never an automatic assignment.</summary>
public sealed record ManagerChoices(IReadOnlyList<ManagerChoice> Items, int TotalCount, int Page, int PageSize, ManagerChoice? AssignedManager);
/// <summary>Infrastructure batches Identity/profile projection inside the caller's existing snapshot.</summary>
public interface IEmployeeDirectoryReader
{
    /// <summary>Filters/counts in SQL, then materializes at most the requested bounded page.</summary>
    Task<EmployeeDirectoryPage> ReadAsync(EmployeeDirectoryFilter filter, CancellationToken token);
    /// <summary>Returns one original-version edit snapshot without loading the collection.</summary>
    Task<GetEmployeeDto?> DetailAsync(Guid id, CancellationToken token);
    /// <summary>Searches active/activated same-department Manager accounts; current assignment is separate.</summary>
    Task<ManagerChoices> ManagersAsync(Guid departmentId, Guid? employeeId, string? search, int page, CancellationToken token);
}
