using NetworkMonitor.Objects;
using NetworkMonitor.Maui.Controls;
namespace NetworkMonitor.Maui.Services
{
    public interface IRootNamespaceProvider
    {
        Type MainActivity { get; }

        IServiceProvider ServiceProvider { get; }
        string GetAppDataDirectory();
        int GetDrawable(string drawableName);
        IColorResource ColorResource{get;}
    }
}
