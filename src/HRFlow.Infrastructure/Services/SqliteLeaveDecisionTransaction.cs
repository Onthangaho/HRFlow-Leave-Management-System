using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>
/// Acquires SQLite's database-wide writer reservation before decision reads, including across
/// processes. Explicit non-deferred transactions avoid validating against a stale read snapshot.
/// </summary>
public sealed class SqliteLeaveDecisionTransaction(HRFlowDbContext context) : ILeaveDecisionTransaction
{
    private const int LockTimeoutSeconds = 3;
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        // Never discard unrelated pending work if this service is accidentally composed into a larger unit of work.
        if (context.ChangeTracker.HasChanges() || context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Leave decisions require a clean, independent unit of work.");
        }

        var connection = (SqliteConnection)context.Database.GetDbConnection();
        var previousDefaultTimeout = connection.DefaultTimeout;
        var previousCommandTimeout = context.Database.GetCommandTimeout();
        connection.DefaultTimeout = LockTimeoutSeconds;
        context.Database.SetCommandTimeout(LockTimeoutSeconds);

        try
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            using var sqliteTransaction = connection.BeginTransaction(deferred: false);
            await using var transaction = await context.Database.UseTransactionAsync(sqliteTransaction, cancellationToken);

            // Controllers may already have resolved an employee using this scoped context.
            // Detach that snapshot so all decision and Identity reads occur after acquiring the lock.
            context.ChangeTracker.Clear();
            await operation(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction!.CommitAsync(cancellationToken);
        }
        catch (SqliteException exception) when (IsContention(exception))
        {
            throw new LeaveDecisionConflictException("Leave decisions are busy. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException sqliteException && IsContention(sqliteException))
        {
            throw new LeaveDecisionConflictException("Leave decisions are busy. Refresh and try again.");
        }
        finally
        {
            // A rolled-back decision must not leave an unsaved status or domain audit entry available for reuse.
            context.ChangeTracker.Clear();
            connection.DefaultTimeout = previousDefaultTimeout;
            context.Database.SetCommandTimeout(previousCommandTimeout);
            await context.Database.CloseConnectionAsync();
        }
    }

    private static bool IsContention(SqliteException exception) =>
        exception.SqliteErrorCode is SqliteBusy or SqliteLocked;
}
