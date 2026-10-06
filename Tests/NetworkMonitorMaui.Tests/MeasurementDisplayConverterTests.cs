using System.Globalization;
using NetworkMonitor.DTOs;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetworkMonitor.Maui.Utils;
using NetworkMonitor.Objects;

namespace NetworkMonitorMaui.Tests;

public class MeasurementDisplayConverterTests : ViewModelTestBase
{
    private readonly MeasurementDisplayConverter _converter = new();
    private object Display(MonitorPingInfo info, string operation = "Average") =>
        _converter.Convert(info, typeof(string), operation, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(32757, 0.1, -3276.8, "A", "-1.1 A")]
    [InlineData(34073, 0.01, -327.68, "V", "13.05 V")]
    [InlineData(123, 1, 0, "ms", "123 ms")]
    [InlineData(7000, 10, 0, "ms", "70000 ms")]
    public void ConvertsAndRoundsPhysicalValues(double sample, double scale, double offset, string unit, string expected)
    {
        var info = new MonitorPingInfo { PacketsRecieved = 1, RoundTripTimeAverage = (float)sample, Scale = scale, Offset = offset, Unit = unit };
        Assert.Equal(expected, Display(info));
        Assert.Equal(sample, info.RoundTripTimeAverage);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(65535, 1)]
    [InlineData(0, 0)]
    public void FailureMarkersAndEmptyDatasetsAreUnavailable(int sample, int received)
    {
        var info = new MonitorPingInfo { PacketsRecieved = received, RoundTripTimeAverage = sample };
        Assert.Equal("Unavailable", Display(info));
    }

    [Fact]
    public void TotalAppliesOffsetForEachValidReading()
    {
        var info = new MonitorPingInfo { PacketsRecieved = 2, RoundTripTimeMinimum = 11, RoundTripTimeMaximum = 13, RoundTripTimeTotal = 24, Scale = 2, Offset = -10, Unit = "ms" };
        Assert.Equal("12 ms", Display(info, "Minimum"));
        Assert.Equal("16 ms", Display(info, "Maximum"));
        Assert.Equal("28 ms", Display(info, "Total"));
    }

    [Fact]
    public void StatusMapCopiesPreserveMeasurementMetadata()
    {
        var source = new MonitorPingInfo { PacketsRecieved = 1, RoundTripTimeAverage = 32757, Scale = 0.1, Offset = -3276.8, Unit = "A" };
        var indicator = new MPIndicator();
        indicator.CopyMonitorPingInfo(source);
        Assert.Equal("-1.1 A", Display(indicator));
    }

    [Fact]
    public void AnimationUsesScaledDurationsAndNeutralSpeedForPhysicalValues()
    {
        var duration = new MonitorPingInfo { EndPointType = "http", PacketsRecieved = 1, RoundTripTimeAverage = 100, Scale = 10, Unit = "ms" };
        Assert.Equal(1000d, Display(duration, "AnimationDuration"));
        Assert.Equal(true, Display(duration, "IsDuration"));
        var current = new MonitorPingInfo { EndPointType = "blebroadcast", Args = "victron --metric battery_current", PacketsRecieved = 1, RoundTripTimeAverage = 32757, Scale = 0.1, Offset = -3276.8, Unit = "A" };
        Assert.Equal(500d, Display(current, "AnimationDuration"));
        Assert.Equal(false, Display(current, "IsDuration"));
    }
    [Fact]
    public void DisabledConnectRetainsRuntimeMeasurementMetadataForTheUi()
    {
        var host = new MonitorPingInfo { MonitorIPID = 75, EndPointType = "customvoltage" };
        var metadata = new EndpointMeasurementMetadata(Unit: "V", Scale: 0.01, Offset: -327.68);
        var connect = new Mock<INetConnect>();
        connect.SetupGet(c => c.MpiStatic).Returns(new MPIStatic(host));
        connect.SetupGet(c => c.IsEnabled).Returns(false);
        connect.SetupGet(c => c.Measurement).Returns(metadata);
        connect.SetupGet(c => c.MeasurementVariants).Returns(Array.Empty<EndpointMeasurementMetadata>());
        var factory = new Mock<IConnectFactory>();
        factory.Setup(f => f.GetNetConnectObj(host, It.IsAny<PingParams>())).Returns(connect.Object);
        var collection = new NetConnectCollection(NullLogger.Instance, CreateNetConnectConfig(), factory.Object);
        collection.Add(host);
        Assert.Equal(metadata, collection.FindMeasurement(75));
        Assert.Null(collection.FindMeasurement(999));
    }

}
