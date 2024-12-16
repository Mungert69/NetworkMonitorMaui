using NetworkMonitor.Objects;

namespace NetworkMonitor.Maui.Services
{
    public static class ServiceInitializer
    {
        public static IRootNamespaceProvider RootProvider { get; private set; }

        public static void Initialize(IRootNamespaceProvider provider)
        {
            RootProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
    }
}
