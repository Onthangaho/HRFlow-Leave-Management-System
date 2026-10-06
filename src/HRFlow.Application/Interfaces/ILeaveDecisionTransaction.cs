namespace HRFlow.Application.Interfaces;

/// <summary>
/// Serializes leave decisions before their reads and commits status and audit changes together.
/// The operation must load its state inside the callback; preloaded entities are not authoritative.
/// </summary>
public interface ILeaveDecisionTransaction
{
    /// <summary>
    /// Runs one decision under database-backed protection. Failed operations persist no changes.
    /// </summary>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
