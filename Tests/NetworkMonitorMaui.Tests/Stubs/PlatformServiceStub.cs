using NetworkMonitor.Objects;

namespace NetworkMonitor.Maui.Services;

public interface IPlatformService
{
    bool RequestPermissionsAsync();
    Task StartBackgroundService();
    Task StopBackgroundService();
    bool IsServiceStarted { get; set; }
    bool IsAuthorised { get; }
    string ServiceMessage { get; set; }
    Task ChangeServiceState(bool state);
    event EventHandler ServiceStateChanged;
    bool DisableAgentOnServiceShutdown { get; set; }
    void OnUpdateServiceState(ResultObj result, bool state);
}

public sealed class FakePlatformService : IPlatformService
{
    public bool IsServiceStarted { get; set; }
    public bool DisableAgentOnServiceShutdown { get; set; }
    public string ServiceMessage { get; set; } = string.Empty;
    public bool RequestPermissionsAsync() => true;
    public Task StartBackgroundService() => Task.CompletedTask;
    public Task StopBackgroundService() => Task.CompletedTask;
    public event EventHandler? ServiceStateChanged;
    public bool IsAuthorised => IsServiceStarted;

    public Task ChangeServiceState(bool state)
    {
        IsServiceStarted = state;
        ServiceMessage = state ? "Agent running" : "Agent stopped";
        ServiceStateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public void OnUpdateServiceState(ResultObj result, bool state)
    {
        IsServiceStarted = state && result.Success;
        ServiceMessage = result.Message;
        ServiceStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
