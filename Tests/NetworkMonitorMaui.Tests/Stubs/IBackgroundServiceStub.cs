using NetworkMonitor.Objects;

namespace NetworkMonitor.Maui.Services;

public interface IBackgroundService
{
    Task<ResultObj> Start();
    Task<ResultObj> Stop();
    bool IsRunning { get; }
}
