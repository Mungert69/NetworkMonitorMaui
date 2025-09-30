using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NetworkMonitor.Connection;
using NetworkMonitor.Maui.Services;
using Xunit;

namespace NetworkMonitorQuantumSecure.Tests;

public class PlatformServiceTests
{
    private static NetConnectConfig CreateConfig()
    {
        var configBuilder = new ConfigurationBuilder();
        return new NetConnectConfig(configBuilder.Build(), appDataDirectory: "./appdata");
    }

    private sealed class TestPlatformService : PlatformService
    {
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }

        public TestPlatformService(NetConnectConfig config)
            : base(NullLogger<PlatformService>.Instance, config)
        {
        }

        public void SetServiceState(bool running)
        {
            _isServiceStarted = running;
        }

        public override Task StartBackgroundService()
        {
            StartCalled = true;
            _isServiceStarted = true;
            return Task.CompletedTask;
        }

        public override Task StopBackgroundService()
        {
            StopCalled = true;
            _isServiceStarted = false;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ChangeServiceState_StartsService_WhenRequestedAndNotRunning()
    {
        var config = CreateConfig();
        config.Owner = "operator"; // important for IsAuthorised check
        var service = new TestPlatformService(config);
        service.SetServiceState(false);

        await service.ChangeServiceState(true);

        Assert.True(service.StartCalled);
        Assert.False(service.StopCalled);
        Assert.True(service.IsServiceStarted);
        Assert.True(service.IsAuthorised);
    }

    [Fact]
    public async Task ChangeServiceState_StopsService_WhenRequestedAndRunning()
    {
        var config = CreateConfig();
        config.Owner = "operator";
        var service = new TestPlatformService(config);
        service.SetServiceState(true);

        await service.ChangeServiceState(false);

        Assert.False(service.StartCalled);
        Assert.True(service.StopCalled);
        Assert.False(service.IsServiceStarted);
    }

    [Fact]
    public async Task ChangeServiceState_NoOp_WhenStateAlreadyMatchesRequest()
    {
        var config = CreateConfig();
        var service = new TestPlatformService(config);
        service.SetServiceState(true);

        await service.ChangeServiceState(true);

        Assert.False(service.StartCalled);
        Assert.False(service.StopCalled);
        Assert.True(service.IsServiceStarted);
    }

    [Theory]
    [InlineData("usersetup", true, false)]
    [InlineData("operator", true, true)]
    [InlineData("usersetup", false, false)]
    [InlineData("operator", false, false)]
    public void IsAuthorised_ReturnsExpectedResult(string owner, bool running, bool expected)
    {
        var config = CreateConfig();
        config.Owner = owner;
        var service = new TestPlatformService(config);
        service.SetServiceState(running);

        Assert.Equal(expected, service.IsAuthorised);
    }
}
