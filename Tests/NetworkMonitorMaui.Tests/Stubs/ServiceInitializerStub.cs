using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NetworkMonitor.Maui.Controls;

namespace NetworkMonitor.Maui.Services;

public interface IRootNamespaceProvider
{
    Type MainActivity { get; }
    IServiceProvider ServiceProvider { get; }
    string GetAppDataDirectory();
    int GetDrawable(string drawableName);
    IColorResource ColorResource { get; }
}

public static class ServiceInitializer
{
    private static IRootNamespaceProvider _rootProvider = new DefaultRootNamespaceProvider();

    public static IRootNamespaceProvider RootProvider => _rootProvider;

    public static void Initialize(IRootNamespaceProvider provider)
    {
        _rootProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    private sealed class DefaultRootNamespaceProvider : IRootNamespaceProvider
    {
        private readonly IServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();
        private readonly IColorResource _colorResource = new DefaultColorResource();

        public Type MainActivity => typeof(object);
        public IServiceProvider ServiceProvider => _serviceProvider;
        public string GetAppDataDirectory() => Path.GetTempPath();
        public int GetDrawable(string drawableName) => 0;
        public IColorResource ColorResource => _colorResource;

        private sealed class DefaultColorResource : IColorResource
        {
            public AppTheme GetRequestedTheme() => AppTheme.Light;
            public Color GetResourceColor(string key) => Colors.White;
            public Color LightenColor(Color color, float factor) => color;
            public void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length) { }
        }
    }
}
