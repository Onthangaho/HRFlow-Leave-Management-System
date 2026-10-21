using System.Diagnostics;

namespace HRFlow.RegressionTests;

/// <summary>Separates native exit from redirected-stream EOF without exposing child output or command arguments.</summary>
internal sealed class FixtureProcessDiagnostics
{
    internal enum Stage { ApiListening, ApiReady, ApiStop, RecoveryListening, RecoveryRefusal, RecoveryStop }
    private readonly Process process;
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private int exited, stdoutEnd, stderrEnd, stdoutLines, stderrLines, unhandled;
    internal FixtureProcessDiagnostics(Process process)
    {
        this.process = process;
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => Interlocked.Exchange(ref exited, 1);
        process.OutputDataReceived += (_, e) => { if (e.Data is null) Interlocked.Exchange(ref stdoutEnd, 1); else Interlocked.Increment(ref stdoutLines); };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) Interlocked.Exchange(ref stderrEnd, 1);
            else { Interlocked.Increment(ref stderrLines); if (e.Data.StartsWith("Unhandled exception.", StringComparison.Ordinal)) Interlocked.Exchange(ref unhandled, 1); }
        };
    }
    internal bool OutputComplete => Volatile.Read(ref stdoutEnd) == 1 && Volatile.Read(ref stderrEnd) == 1;
    internal void Write(Stage stage)
    {
        var hasExited = process.HasExited;
        long cpu = -1, memory = -1; int threads = -1;
        try { process.Refresh(); cpu = (long)process.TotalProcessorTime.TotalMilliseconds; if (!hasExited) { memory = process.WorkingSet64 / (1024 * 1024); threads = process.Threads.Count; } }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        Console.WriteLine($"fixture-process: stage={stage}; exited={hasExited}; exit-event={Volatile.Read(ref exited)}; stdout-eof={Volatile.Read(ref stdoutEnd)}; stderr-eof={Volatile.Read(ref stderrEnd)}; stdout-lines={Volatile.Read(ref stdoutLines)}; stderr-lines={Volatile.Read(ref stderrLines)}; unhandled={Volatile.Read(ref unhandled)}; elapsed-ms={elapsed.ElapsedMilliseconds}; cpu-ms={cpu}; memory-mib={memory}; threads={threads}");
    }
}
