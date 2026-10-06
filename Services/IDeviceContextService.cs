namespace NetworkMonitor.Maui.Services;

public interface IDeviceContextService
{
    Task RefreshAndPersistAsync();
}
