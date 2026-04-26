#if ANDROID
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace NetworkMonitor.Maui.Helpers;

internal sealed class AndroidMemoryTelemetryHelper : IDisposable
{
    private readonly ILogger _logger;
    private int _isSampling;

    public AndroidMemoryTelemetryHelper(ILogger logger)
    {
        _logger = logger;
    }

    public void Sample()
    {
        if (Interlocked.Exchange(ref _isSampling, 1) == 1)
        {
            return;
        }

        try
        {
            LogMemoryTelemetry();
        }
        finally
        {
            Volatile.Write(ref _isSampling, 0);
        }
    }

    public void Dispose()
    {
        // Nothing to dispose. Sampling is driven by the processor polling loop.
    }

    private void LogMemoryTelemetry()
    {
        try
        {
            var managedBytes = GC.GetTotalMemory(forceFullCollection: false);
            var status = ReadProcStatus();
            var threadPoolThreads = ThreadPool.ThreadCount;

            _logger.LogInformation(
                "MEMORY TELEMETRY: managed={ManagedMB:F1}MB workingSet={WorkingSetMB:F1}MB vmData={VmDataMB:F1}MB threads={Threads} threadPool={ThreadPoolThreads}",
                BytesToMb(managedBytes),
                KbToMb(status.WorkingSetKb),
                KbToMb(status.VmDataKb),
                status.Threads,
                threadPoolThreads);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect memory telemetry.");
        }
    }

    private static (int WorkingSetKb, int VmDataKb, int Threads) ReadProcStatus()
    {
        const string statusPath = "/proc/self/status";
        int workingSetKb = -1;
        int vmDataKb = -1;
        int threads = -1;

        foreach (var line in File.ReadLines(statusPath))
        {
            if (line.StartsWith("VmRSS:", StringComparison.Ordinal))
            {
                workingSetKb = ParseLeadingInt(line);
            }
            else if (line.StartsWith("VmData:", StringComparison.Ordinal))
            {
                vmDataKb = ParseLeadingInt(line);
            }
            else if (line.StartsWith("Threads:", StringComparison.Ordinal))
            {
                threads = ParseLeadingInt(line);
            }
        }

        return (workingSetKb, vmDataKb, threads);
    }

    private static int ParseLeadingInt(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return -1;
        }

        return int.TryParse(parts[1], out var value) ? value : -1;
    }

    private static double BytesToMb(long value)
    {
        if (value < 0)
        {
            return -1;
        }

        return value / 1024d / 1024d;
    }

    private static double KbToMb(int value)
    {
        if (value < 0)
        {
            return -1;
        }

        return value / 1024d;
    }
}
#endif
