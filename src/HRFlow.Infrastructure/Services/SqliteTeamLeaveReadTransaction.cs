using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>Starts a deferred, read-only snapshot; unlike protected writes this does not issue BEGIN IMMEDIATE.</summary>
public sealed class SqliteTeamLeaveReadTransaction(HRFlowDbContext context) : ITeamLeaveReadTransaction, ILeaveReportingReadTransaction
{
    private const int TimeoutSeconds = 3;
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        if (context.ChangeTracker.HasChanges() || context.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Leave reporting reads require an independent unit of work.");

        var connection = (SqliteConnection)context.Database.GetDbConnection();
        var previousTimeout = connection.DefaultTimeout;
        var previousCommandTimeout = context.Database.GetCommandTimeout();
        connection.DefaultTimeout = TimeoutSeconds;
        context.Database.SetCommandTimeout(TimeoutSeconds);
        try
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            using var snapshot = connection.BeginTransaction(deferred: true);
            await using var transaction = await context.Database.UseTransactionAsync(snapshot, cancellationToken);
            // The first authoritative SELECT establishes the snapshot; no tracked controller entity is reused.
            var result = await read(cancellationToken);
            if (context.ChangeTracker.HasChanges())
                throw new InvalidOperationException("Leave reporting snapshots must not contain writes.");
            await transaction!.CommitAsync(cancellationToken);
            return result;
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is SqliteBusy or SqliteLocked)
        {
            throw new WriteConflictException("Leave reporting is temporarily busy. Refresh to try again.");
        }
        finally
        {
            connection.DefaultTimeout = previousTimeout;
            context.Database.SetCommandTimeout(previousCommandTimeout);
            await context.Database.CloseConnectionAsync();
        }
    }
}
