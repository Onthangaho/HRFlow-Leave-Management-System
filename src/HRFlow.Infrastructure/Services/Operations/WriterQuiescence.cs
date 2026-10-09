using System.Diagnostics;
using System.Text;

namespace HRFlow.Infrastructure.Services.Operations;

/// <summary>Verifies named API/worker processes are stopped; a file lease then prevents cooperating restarts.</summary>
public static class WriterQuiescence
{
    /// <summary>Fails closed when process inventory is unavailable; does not terminate services or trust a supplied PID list.</summary>
    public static void RequireStopped()
    {
        if (OperatingSystem.IsWindows())
        {
            const string script = """$ErrorActionPreference='Stop';$processes=@(Get-CimInstance Win32_Process);if (@($processes | Where-Object { $_.Name -eq 'dotnet.exe' -and [string]::IsNullOrWhiteSpace($_.CommandLine) }).Count -gt 0) { throw 'Process inventory unavailable' };$writers=@($processes | Where-Object { $_.Name -eq 'HRFlow.Api.exe' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'HRFlow[.]Api[.]dll(?:[" ]|$)') });Write-Output $writers.Count""";
            var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-EncodedCommand");
            start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Process inventory unavailable.");
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(10000)) { process.Kill(true); throw new InvalidOperationException("Process inventory unavailable."); }
            if (process.ExitCode != 0 || !int.TryParse(output.GetAwaiter().GetResult().Trim(), out var count) || count != 0)
                throw new InvalidOperationException("Stop every HRFlow API/worker process and disable service auto-restarts.");
            _ = errors.GetAwaiter().GetResult();
        }
        else if (OperatingSystem.IsLinux())
        {
            foreach (var process in Process.GetProcesses())
                using (process)
                {
                    try
                    {
                        if (process.ProcessName is not ("dotnet" or "HRFlow.Api")) continue;
                        var arguments = File.ReadAllText($"/proc/{process.Id}/cmdline").Split('\0');
                        if (arguments.Any(argument => Path.GetFileName(argument) is "HRFlow.Api.dll" or "HRFlow.Api"))
                            throw new InvalidOperationException("Stop every HRFlow API/worker process and disable service auto-restarts.");
                    }
                    catch (DirectoryNotFoundException) { /* A process that exited during inventory is no longer a writer. */ }
                }
        }
        else throw new InvalidOperationException("This operator process inventory is supported on Windows/Linux only.");
    }
}
