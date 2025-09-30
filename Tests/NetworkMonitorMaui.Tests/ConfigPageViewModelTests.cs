using System.Collections.Generic;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Objects;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class ConfigPageViewModelTests : ViewModelTestBase
{
    [Fact]
    public void ExposesUnderlyingNetConfigValues()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["BaseFusionAuthURL"] = "https://fusion.example.com",
            ["ClientId"] = "client-xyz",
            ["AppID"] = "app-42",
            ["MonitorLocation"] = "UnitTest"
        };

        var config = CreateNetConnectConfig(overrides);
        config.AuthKey = "auth-key";
        config.OqsProviderPath = "/oqs";
        config.AgentUserFlow.IsAuthorized = true;

        var viewModel = new ConfigPageViewModel(GetLogger<ConfigPageViewModel>(), config);

        Assert.Equal("https://fusion.example.com", viewModel.BaseFusionAuthURL);
        Assert.Equal("client-xyz", viewModel.ClientId);
        Assert.Equal("app-42", viewModel.AppID);
        Assert.Equal("UnitTest", viewModel.MonitorLocation);
        Assert.Equal("auth-key", viewModel.AuthKey);
    }
}
