using System.Net;
using System.Net.Sockets;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Isolates the deliberate refusal and proves owned resources are released even when the caller fails.</summary>
public sealed class RecoveryLifecycleTests
{
    /// <summary>Runs the exact Production refusal without the backup/resend workflow; no SMTP substitution is installed.</summary>
    [Fact]
    public async Task ProductionPickupRefusalExitsAndDrainsOutput()
    {
        await using var api = await ApiFixture.StartAsync();
        var authentication = api.AuthenticationEnvironment(); await api.StopAsync();
        var fixture = await ActivationRecoveryFixture.CreateAsync(authentication);
        try { _ = await fixture.StartHostAsync(api.DatabasePath, api.PrivateRoot, production: true, forbiddenPickup: true); }
        finally { await fixture.DisposeAsync(); }
        Assert.True(fixture.Cleaned);
    }

    /// <summary>Failure before/after normal startup must still release the listener, process, database writer and generated files.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerFailureStillCleansOwnedResources(bool afterStartup)
    {
        await using var api = await ApiFixture.StartAsync();
        var authentication = api.AuthenticationEnvironment(); await api.StopAsync();
        var fixture = await ActivationRecoveryFixture.CreateAsync(authentication);
        var port = fixture.Smtp.Port;
        await Assert.ThrowsAsync<InjectedCallerFailure>(async () =>
        {
            await using (fixture)
            {
                if (afterStartup) _ = await fixture.StartHostAsync(api.DatabasePath, api.PrivateRoot);
                throw new InjectedCallerFailure();
            }
        });
        Assert.True(fixture.Cleaned); Assert.Equal(0, fixture.Smtp.ActiveConnections);
        using var socket = new TcpClient();
        await Assert.ThrowsAsync<SocketException>(async () => await socket.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromSeconds(3)));
        using var connection = api.Connection();
        using var transaction = connection.BeginTransaction(deferred: false);
        transaction.Rollback();
    }
    private sealed class InjectedCallerFailure : Exception { }
}
