namespace HRFlow.Application.Interfaces;

/// <summary>Coordinates policy/type writes and submissions with decisions before authoritative reads.</summary>
public interface ILeaveConfigurationTransaction
{
    /// <summary>Reserves the writer, runs fresh validation, and commits all changes atomically.</summary>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
