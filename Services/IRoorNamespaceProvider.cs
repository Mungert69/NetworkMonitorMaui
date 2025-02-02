using NetworkMonitor.Objects;
namespace NetworkMonitor.Maui.Services
{
    public interface IRootNamespaceProvider
    {
        Type MainActivity { get; }

        IServiceProvider ServiceProvider { get; }
        string GetAppDataDirectory();
        int GetDrawable(string drawableName);
        ColorResource ColorResource{get;}
    }
}
