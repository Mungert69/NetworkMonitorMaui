#if ANDROID
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NetworkMonitor.Maui.Helpers;

internal sealed class AndroidMemoryTelemetryHelper : IDisposable
{
    private readonly ILogger _logger;
    private readonly TimeSpan _interval;
    private System.Timers.Timer? _timer;

    public AndroidMemoryTelemetryHelper(ILogger logger, TimeSpan interval)
    {
        _logger = logger;
        _interval = interval;
    }

    public void Start()
    {
        try
        {
            Stop();
            _timer = new System.Timers.Timer(_interval.TotalMilliseconds);
            _timer.AutoReset = true;
            _timer.Elapsed += (_, _) => LogMemoryTelemetry();
            _timer.Start();
            LogMemoryTelemetry();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start memory telemetry timer.");
        }
    }

    public void Stop()
    {
        try
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop memory telemetry timer.");
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void LogMemoryTelemetry()
    {
        try
        {
            var managedBytes = GC.GetTotalMemory(forceFullCollection: false);
            var proc = Process.GetCurrentProcess();
            var workingSetBytes = proc.WorkingSet64;

            long javaUsedBytes = -1;
            long javaMaxBytes = -1;
            try
            {
                var runtime = Java.Lang.Runtime.GetRuntime();
                javaUsedBytes = runtime.TotalMemory() - runtime.FreeMemory();
                javaMaxBytes = runtime.MaxMemory();
            }
            catch
            {
                // Best-effort telemetry only.
            }

            int totalPssKb = -1;
            int dalvikPssKb = -1;
            int nativePssKb = -1;
            int otherPssKb = -1;
            try
            {
                using var memInfo = new Android.OS.Debug.MemoryInfo();
                Android.OS.Debug.GetMemoryInfo(memInfo);
                totalPssKb = memInfo.TotalPss;
                dalvikPssKb = memInfo.DalvikPss;
                nativePssKb = memInfo.NativePss;
                otherPssKb = memInfo.OtherPss;
            }
            catch
            {
                // Best-effort telemetry only.
            }

            _logger.LogInformation(
                "MEMORY TELEMETRY: managed={ManagedMB:F1}MB workingSet={WorkingSetMB:F1}MB javaUsed={JavaUsedMB:F1}MB javaMax={JavaMaxMB:F1}MB pssTotal={PssTotalMB:F1}MB pssDalvik={PssDalvikMB:F1}MB pssNative={PssNativeMB:F1}MB pssOther={PssOtherMB:F1}MB",
                BytesToMb(managedBytes),
                BytesToMb(workingSetBytes),
                BytesToMb(javaUsedBytes),
                BytesToMb(javaMaxBytes),
                KbToMb(totalPssKb),
                KbToMb(dalvikPssKb),
                KbToMb(nativePssKb),
                KbToMb(otherPssKb));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect memory telemetry.");
        }
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
