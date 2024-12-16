using NetworkMonitor.Objects;
namespace NetworkMonitor.Maui.Services
{
  public interface IPlatformService
    {
        bool RequestPermissionsAsync();
        Task StartBackgroundService();
        Task StopBackgroundService();
        bool IsServiceStarted { get; set; }
        string ServiceMessage { get; set; }
        Task ChangeServiceState(bool state);
        //void OnServiceStateChanged();
        event EventHandler ServiceStateChanged;
        bool DisableAgentOnServiceShutdown { get; set; }
        void OnUpdateServiceState(ResultObj result, bool state);
    }
}
  