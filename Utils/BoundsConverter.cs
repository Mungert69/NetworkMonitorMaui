using System;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.DTOs;

namespace NetworkMonitor.Maui.Utils;
 public class BoundsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MPIndicator data) // Replace YourDataType with the actual type of your data item
        {
            // Assuming you have properties like PositionX and PositionY in your data item
            double x = data.XPosition; // Replace with your actual property
            double y = data.YPosition; // Replace with your actual property

            return new Rect(x, y, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize);
        }

        return Rect.Zero;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
