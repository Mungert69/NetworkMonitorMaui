using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NetworkMonitor.Maui.Controls;
using NetworkMonitor.Maui.Services;
using Xunit;

namespace NetworkMonitorQuantumSecure.Tests;

public class ServiceInitializerTests
{
    private sealed class StubRootProvider : IRootNamespaceProvider
    {
        public IServiceProvider ServiceProvider => new ServiceCollection().BuildServiceProvider();
        public string GetAppDataDirectory() => "/tmp";
        public IColorResource ColorResource => new StubColorResource();
        public Type MainActivity => typeof(object);
        public int GetDrawable(string drawableName) => 0;

        private sealed class StubColorResource : IColorResource
        {
            public AppTheme GetRequestedTheme() => AppTheme.Light;
            public Microsoft.Maui.Graphics.Color GetResourceColor(string key) => Microsoft.Maui.Graphics.Colors.White;
            public Microsoft.Maui.Graphics.Color LightenColor(Microsoft.Maui.Graphics.Color color, float factor) => color;
            public void AnimateColor(BoxView boxView, Microsoft.Maui.Graphics.Color fromColor, Microsoft.Maui.Graphics.Color toColor, uint length) { }
        }
    }

    [Fact]
    public void Initialize_SetsRootProvider()
    {
        var provider = new StubRootProvider();

        ServiceInitializer.Initialize(provider);

        Assert.Same(provider, ServiceInitializer.RootProvider);
    }

    [Fact]
    public void Initialize_NullProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceInitializer.Initialize(null!));
    }
}
