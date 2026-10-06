namespace HRFlow.Application.Interfaces;

/// <summary>Protects profile, Identity, role, and reporting reads/writes across API processes as one atomic operation.</summary>
public interface IEmployeeManagementTransaction
{
    /// <summary>Acquires database writer protection before the callback reads any management state.</summary>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
