using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Fixture privacy is a prerequisite for exercising real production image and recovery checks on Linux.</summary>
public sealed class FixturePermissionTests
{
    /// <summary>Detects permissive children and database sidecars even when a private parent masks the mistake.</summary>
    [Fact]
    public async Task GeneratedStorageIsExplicitlyOwnerOnlyBeforeAndAfterRestart()
    {
        await using var fixture = await ApiFixture.StartAsync();
        foreach (var path in new[] { Path.GetDirectoryName(fixture.DatabasePath)!, fixture.PrivateRoot, fixture.DatabasePath })
            ApiFixture.AssertPrivatePath(path);
        // SQLite sidecars inherit the restricted database mode, not the runner's process umask.
        using (var connection = fixture.Connection())
        using (var transaction = connection.BeginTransaction())
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "UPDATE Employees SET FullName=FullName"; command.ExecuteNonQuery();
            foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
                if (File.Exists(fixture.DatabasePath + suffix)) ApiFixture.AssertPrivatePath(fixture.DatabasePath + suffix);
            transaction.Rollback();
        }
        await fixture.RestartAsync();
        foreach (var path in new[] { fixture.PrivateRoot, fixture.DatabasePath }) ApiFixture.AssertPrivatePath(path);
    }
}
