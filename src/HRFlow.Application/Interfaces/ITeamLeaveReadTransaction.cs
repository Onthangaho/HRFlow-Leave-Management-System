namespace HRFlow.Application.Interfaces;

/// <summary>Keeps current manager authorization and team projection in one read snapshot without reserving a writer.</summary>
public interface ITeamLeaveReadTransaction
{
    /// <summary>Runs only reads; provider-specific snapshot handling stays in Infrastructure.</summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken cancellationToken);
}
