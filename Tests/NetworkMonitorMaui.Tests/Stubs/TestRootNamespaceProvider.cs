using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NetworkMonitor.Maui.Controls;

namespace NetworkMonitor.Maui.Services;

public sealed class TestRootNamespaceProvider : IRootNamespaceProvider
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();
    private readonly IColorResource _colorResource;

    public TestRootNamespaceProvider(IColorResource? colorResource = null)
    {
        _colorResource = colorResource ?? new TestColorResource();
    }

    public Type MainActivity => typeof(object);
    public IServiceProvider ServiceProvider => _serviceProvider;
    public string GetAppDataDirectory() => Path.GetTempPath();
    public int GetDrawable(string drawableName) => 0;
    public IColorResource ColorResource => _colorResource;
}

public sealed class TestColorResource : IColorResource
{
    private readonly Dictionary<string, Color> _colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Warning"] = Colors.Yellow,
        ["Primary"] = Colors.Blue,
        ["Gray950"] = Colors.Black
    };

    public AppTheme GetRequestedTheme() => AppTheme.Light;

    public Color GetResourceColor(string key)
        => _colors.TryGetValue(key, out var color) ? color : Colors.White;

    public Color LightenColor(Color color, float factor)
    {
        factor = Math.Max(0, factor);
        return new Color(
            (float)Math.Min(color.Red + factor, 1.0f),
            (float)Math.Min(color.Green + factor, 1.0f),
            (float)Math.Min(color.Blue + factor, 1.0f),
            (float)color.Alpha);
    }

    public void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length)
    {
        boxView.Color = toColor;
    }
}
