using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>
/// Acquires SQLite's database-wide writer reservation before write-validation reads, including across
/// processes. Explicit non-deferred transactions avoid validating against a stale read snapshot.
/// </summary>
public sealed class SqliteWriteTransaction(HRFlowDbContext context) : ILeaveDecisionTransaction, IEmployeeManagementTransaction, ILeaveConfigurationTransaction
{
    private const int LockTimeoutSeconds = 3;
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int SqliteUniqueConstraint = 2067;
    private const int SqliteForeignKeyConstraint = 787;
    private const int SqliteTriggerConstraint = 1811;

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        // Never discard unrelated pending work if this service is accidentally composed into a larger unit of work.
        if (context.ChangeTracker.HasChanges() || context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Protected writes require a clean, independent unit of work.");
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
        catch (DbUpdateConcurrencyException)
        {
            throw new WriteConflictException("This record changed. Reload it before saving again.");
        }
        catch (SqliteException exception) when (IsContention(exception))
        {
            throw new WriteConflictException("Updates are busy. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException sqliteException && IsContention(sqliteException))
        {
            throw new WriteConflictException("Updates are busy. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException unique
            && unique.SqliteExtendedErrorCode == SqliteUniqueConstraint
            && unique.Message.Contains("LeaveTypes.NormalizedName", StringComparison.Ordinal))
        {
            throw new WriteConflictException("A leave type with this name already exists. Choose another name.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException reference && IsReferenceConstraint(reference)
            && exception.Entries.Any(entry => entry.Entity is LeaveType or LeavePolicy or LeaveRequest))
        {
            // Foreign keys are the final safeguard against reference races from writers outside this protocol.
            throw new WriteConflictException("Leave configuration references changed or are still in use. Reload before trying again.");
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

    private static bool IsReferenceConstraint(SqliteException exception) =>
        // SQLite's RESTRICT action reports a trigger constraint, unlike a missing-parent insert.
        exception.SqliteExtendedErrorCode is SqliteForeignKeyConstraint or SqliteTriggerConstraint
        && exception.Message.Contains("FOREIGN KEY constraint failed", StringComparison.Ordinal);
}
