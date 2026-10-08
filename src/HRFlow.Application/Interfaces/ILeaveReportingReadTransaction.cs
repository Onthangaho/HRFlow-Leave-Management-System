namespace HRFlow.Application.Interfaces;

/// <summary>Keeps live reporting/history authorization and read inputs in one snapshot without reserving the writer.</summary>
public interface ILeaveReportingReadTransaction
{
    /// <summary>Runs report reads on the scoped store; provider-specific isolation stays in Infrastructure.</summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken cancellationToken);
}
