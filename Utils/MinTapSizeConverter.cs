using System;
using System.Globalization;
using Microsoft.Maui.Controls;
namespace NetworkMonitor.Maui.Utils;

public class MinTapSizeConverter : IValueConverter
{
    private const double MinTapSize = 44; // Recommended minimum touch target
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double diameter)
            return Math.Max(diameter, MinTapSize);
        return MinTapSize;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
