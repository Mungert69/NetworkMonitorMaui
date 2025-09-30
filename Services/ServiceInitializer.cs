using NetworkMonitor.Objects;

namespace NetworkMonitor.Maui.Services
{
    public static class ServiceInitializer
    {
        private static IRootNamespaceProvider? _rootProvider;
        private static IUiDispatcher? _dispatcher;

        public static IRootNamespaceProvider RootProvider =>
            _rootProvider ?? throw new InvalidOperationException("ServiceInitializer has not been initialized. Call ServiceInitializer.Initialize during app startup.");

        public static IUiDispatcher Dispatcher =>
            _dispatcher ??= new MainThreadDispatcher();

        public static void Initialize(IRootNamespaceProvider provider, IUiDispatcher? dispatcher = null)
        {
            _rootProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (dispatcher != null)
            {
                _dispatcher = dispatcher;
            }
        }

        public static void SetDispatcher(IUiDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }
    }
}
