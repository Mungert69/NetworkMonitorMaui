using System.Globalization;
using Microsoft.Maui.Controls;
using NetworkMonitor.Connection;
using NetworkMonitor.Objects;
using NetworkMonitor.Utils.Helpers;

namespace NetworkMonitor.Maui.Utils;

/// <summary>Formats encoded host statistics at the UI boundary without changing stored readings.</summary>
public sealed class MeasurementDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var operation = parameter?.ToString() ?? "Average";
        if (value is not MonitorPingInfo info)
            return operation == "IsDuration" ? false : operation == "AnimationDuration" ? 500d : "Unavailable";

        var args = string.IsNullOrWhiteSpace(info.Args) ? info.Username : info.Args;
        var isDuration = info.Unit == "ms"
            && EndpointMeasurementDefaults.Get(info.EndPointType, args).AnalysisKind == "duration";
        if (operation == "IsDuration") return isDuration;
        double? sample = info.PacketsRecieved > 0 ? operation switch
        {
            "Maximum" => MeasurementConversion.Value(info.RoundTripTimeMaximum, info.Scale, info.Offset),
            "Minimum" => MeasurementConversion.Value(info.RoundTripTimeMinimum, info.Scale, info.Offset),
            "Total" => MeasurementConversion.Total(info.RoundTripTimeTotal, info.PacketsRecieved, info.Scale, info.Offset),
            _ => MeasurementConversion.Value(info.RoundTripTimeAverage, info.Scale, info.Offset),
        } : null;
        // Physical values such as current must not determine a latency animation.
        if (operation == "AnimationDuration") return isDuration ? sample ?? 500d : 500d;
        if (!sample.HasValue) return "Unavailable";
        var number = sample.Value.ToString("0.######", culture);
        return string.IsNullOrWhiteSpace(info.Unit) ? number : $"{number} {info.Unit}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
