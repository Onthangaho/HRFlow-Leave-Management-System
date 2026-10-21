using System.Diagnostics;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services.Auth;
using HRFlow.Infrastructure.Services.Operations;
using HRFlow.RegressionTests;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>Loaded only by an explicitly launched test child through DOTNET_STARTUP_HOOKS; production binaries contain no fixture switch.</summary>
public static class StartupHook
{
    /// <summary>Observes the real Program/DI and worker events without adding an endpoint or bypassing authorization.</summary>
    public static void Initialize()
    {
        var root = Environment.GetEnvironmentVariable("HRFLOW_RECOVERY_TEST_ROOT") ?? throw new InvalidOperationException("Private recovery fixture required.");
        if (Path.GetDirectoryName(root) != Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)
            || !Path.GetFileName(root).StartsWith("hrflow-activation-recovery-", StringComparison.Ordinal))
            throw new InvalidOperationException("Private recovery fixture required.");
        PrivateOperationsPaths.RequireRestricted(root);
        var port = int.Parse(Environment.GetEnvironmentVariable("HRFLOW_RECOVERY_TEST_SMTP")!);
        DiagnosticListener.AllListeners.Subscribe(new Listeners(port, Guid.TryParse(Environment.GetEnvironmentVariable("HRFLOW_RECOVERY_TEST_EVENT"), out var id) ? id : null));
    }
    private sealed class Listeners(int port, Guid? sentinel) : IObserver<DiagnosticListener>
    {
        public void OnNext(DiagnosticListener listener)
        { if (listener.Name is "Microsoft.Extensions.Hosting" or "Microsoft.EntityFrameworkCore") listener.Subscribe(new Events(port, sentinel)); }
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
    private sealed class Events(int port, Guid? sentinel) : IObserver<KeyValuePair<string, object?>>
    {
        private static int read;
        private static int committed;
        private readonly HashSet<Guid> workers = [];
        public void OnNext(KeyValuePair<string, object?> entry)
        {
            if (entry.Key == "HostBuilding" && entry.Value is IHostBuilder builder)
                builder.ConfigureServices((context, services) =>
                {
                    if (!context.HostingEnvironment.IsDevelopment()) throw new InvalidOperationException("Private SMTP fixture is Development-only.");
                    services.RemoveAll<IActivationDelivery>();
                    services.AddSingleton(PrivateActivationSmtp.Transport(port));
                    Console.WriteLine("test-smtp-installed");
                });
            if (entry.Value is CommandExecutedEventData command && command.Command.CommandText.Contains("\"LeaveNotificationEvents\"", StringComparison.Ordinal))
            {
                if (Interlocked.Exchange(ref read, 1) == 0) Console.WriteLine("test-worker-read");
                if (sentinel is { } id && command.Command.Parameters.Cast<System.Data.Common.DbParameter>()
                    .Any(p => Guid.TryParse(Convert.ToString(p.Value), out var value) && value == id) && command.Context is { } db)
                    lock (workers) workers.Add(db.ContextId.InstanceId);
            }
            if (entry.Value is TransactionEndEventData transaction && entry.Key.EndsWith("TransactionCommitted", StringComparison.Ordinal)
                && transaction.Context is { } contextDb)
            {
                bool worker; lock (workers) worker = workers.Remove(contextDb.ContextId.InstanceId);
                if (worker && Interlocked.Exchange(ref committed, 1) == 0) Console.WriteLine("test-worker-commit");
            }
        }
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}
