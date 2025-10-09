namespace NetworkMonitor.Maui.Services;

public static partial class ServiceInitializer
{
    internal static void ResetForTests()
    {
        _rootProvider = null;
        _dispatcher = null;
    }
}
