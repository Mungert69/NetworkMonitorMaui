using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using Moq;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Connection;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Objects;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class ScanProcessorStatesViewModelTests : ViewModelTestBase
{
    private sealed class StubCmdProcessorStates : ILocalCmdProcessorStates
    {
        public ConcurrentBag<MonitorIP> ActiveDevices { get; set; } = new();
        public ConcurrentBag<PingInfo> PingInfos { get; set; } = new();
        public string DefaultEndpointType { get; set; } = "icmp";
        public List<string> EndpointTypes { get; set; } = new();
        public bool UseDefaultEndpointType { get; set; }
        public bool UseFastScan { get; set; }
        public bool LimitPorts { get; set; }
        public List<MonitorIP> SelectedDevices { get; set; } = new();
        public NetworkInterfaceInfo SelectedNetworkInterface { get; set; } = new();
        public List<NetworkInterfaceInfo> AvailableNetworkInterfaces { get; set; } = new();
        public string CompletedMessage { get; set; } = string.Empty;
        public string RunningMessage { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsCmdRunning { get; set; }
        public bool IsCmdSuccess { get; set; }
        public bool IsCmdAvailable { get; set; } = true;
        public string CmdName { get; set; } = "Nmap";
        public string CmdDisplayName { get; set; } = "Nmap";

        public event Func<Task>? OnStartScanAsync;
        public event Func<Task>? OnCancelScanAsync;
        public event Func<Task>? OnAddServicesAsync;
        public event PropertyChangedEventHandler? PropertyChanged;

        public Task Scan()
        {
            IsRunning = true;
            return OnStartScanAsync?.Invoke() ?? Task.CompletedTask;
        }

        public Task Cancel() => OnCancelScanAsync?.Invoke() ?? Task.CompletedTask;
        public Task AddServices() => OnAddServicesAsync?.Invoke() ?? Task.CompletedTask;
        public void Init() { }
    }

    private static ScanProcessorStatesViewModel CreateViewModel(
        StubCmdProcessorStates states,
        NetConnectConfig config,
        out Mock<ICmdProcessorProvider> providerMock)
    {
        providerMock = new Mock<ICmdProcessorProvider>();
        providerMock.Setup(p => p.GetProcessorStates("Nmap")).Returns(states);
        var apiServiceMock = new Mock<IApiService>();

        return new ScanProcessorStatesViewModel(
            GetLogger<ScanProcessorStatesViewModel>(),
            providerMock.Object,
            apiServiceMock.Object,
            config);
    }

    [Fact]
    public void ConstructorLoadsEndpointTypes()
    {
        var config = CreateNetConnectConfig();
        config.EndpointTypes = new List<string> { "icmp", "http" };
        var states = new StubCmdProcessorStates { EndpointTypes = config.EndpointTypes };

        _ = CreateViewModel(states, config, out _);

        Assert.Equal(config.EndpointTypes, states.EndpointTypes);
    }

    [Fact]
    public void AddSelectedHosts_ReplacesSelection()
    {
        var config = CreateNetConnectConfig();
        var states = new StubCmdProcessorStates { EndpointTypes = config.EndpointTypes };

        var viewModel = CreateViewModel(states, config, out _);

        var devices = new List<MonitorIP>
        {
            new() { ID = 1, Address = "10.0.0.1", EndPointType = "icmp", Port = 0 }
        };

        viewModel.AddSelectedHosts(devices);

        Assert.Single(states.SelectedDevices);
        Assert.Equal("10.0.0.1", states.SelectedDevices[0].Address);
    }
}
