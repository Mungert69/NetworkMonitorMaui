using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NetworkMonitor.Maui.Controls;
using NetworkMonitor.Maui.Services;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class ServiceInitializerTests : IDisposable
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
            public Color GetResourceColor(string key) => Colors.White;
            public Color LightenColor(Color color, float factor) => color;
            public void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length) { }
        }
    }

    public ServiceInitializerTests()
    {
        ServiceInitializer.ResetForTests();
    }

    [Fact]
    public void Initialize_SetsRootProvider()
    {
        var provider = new StubRootProvider();

        ServiceInitializer.Initialize(provider);

        Assert.Same(provider, ServiceInitializer.RootProvider);
    }

    [Fact]
    public void Initialize_WithDispatcher_SetsDispatcher()
    {
        var provider = new StubRootProvider();
        var dispatcher = new TestDispatcher();

        ServiceInitializer.Initialize(provider, dispatcher);

        Assert.Same(dispatcher, ServiceInitializer.Dispatcher);
    }

    [Fact]
    public void SetDispatcher_ReplacesExistingDispatcher()
    {
        var provider = new StubRootProvider();
        ServiceInitializer.Initialize(provider, new TestDispatcher());
        var replacement = new TestDispatcher();

        ServiceInitializer.SetDispatcher(replacement);

        Assert.Same(replacement, ServiceInitializer.Dispatcher);
    }

    [Fact]
    public void Dispatcher_DefaultsToMainThreadDispatcher()
    {
        var dispatcher = ServiceInitializer.Dispatcher;

        Assert.IsType<MainThreadDispatcher>(dispatcher);
    }

    [Fact]
    public void Initialize_NullProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceInitializer.Initialize(null!));
    }

    public void Dispose()
    {
        ServiceInitializer.ResetForTests();
    }
}
