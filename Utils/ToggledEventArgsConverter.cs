using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace NetworkMonitor.Maui.Utils;
public class ToggledEventArgsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ToggledEventArgs toggledEventArgs)
        {
            return toggledEventArgs.Value;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
