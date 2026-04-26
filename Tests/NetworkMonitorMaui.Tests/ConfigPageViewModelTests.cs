using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NetworkMonitor.Maui.Services;
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

        var viewModel = new ConfigPageViewModel(
            GetLogger<ConfigPageViewModel>(),
            config,
            new TestDialogService(),
            new LocalProcessorStates(),
            new FakePlatformService(),
            appDataDirectory: Path.GetTempPath());

        Assert.Equal("https://fusion.example.com", viewModel.BaseFusionAuthURL);
        Assert.Equal("client-xyz", viewModel.ClientId);
        Assert.Equal("app-42", viewModel.AppID);
        Assert.Equal("UnitTest", viewModel.MonitorLocation);
        Assert.Equal("auth-key", viewModel.AuthKey);
    }

    [Fact]
    public void NetConfigPropertyChange_PropagatesThroughDispatcher()
    {
        var config = CreateNetConnectConfig();
        var dispatcher = new TestDispatcher();
        var viewModel = new ConfigPageViewModel(
            GetLogger<ConfigPageViewModel>(),
            config,
            new TestDialogService(),
            new LocalProcessorStates(),
            new FakePlatformService(),
            dispatcher,
            appDataDirectory: Path.GetTempPath());

        string? observedProperty = null;
        viewModel.PropertyChanged += (_, args) => observedProperty ??= args.PropertyName;

        config.ClientId = "updated-client";

        Assert.Equal(nameof(ConfigPageViewModel.ClientId), observedProperty);
        Assert.Equal(1, dispatcher.DispatchCalls);
    }

    [Fact]
    public async Task ResetToDefaults_RemovesFilesAndUpdatesState()
    {
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        try
        {
            var config = CreateNetConnectConfig();
            config.AuthKey = "auth";
            config.RabbitPassword = "rabbit";
            config.AgentUserFlow.IsAuthorized = true;

            var states = new LocalProcessorStates
            {
                IsRunning = true,
                IsSetup = true,
                IsRabbitConnected = true,
                IsConnectState = ConnectState.Running,
                RunningMessage = "Running",
                SetupMessage = "Setup",
                RabbitSetupMessage = "Rabbit",
                ConnectRunningMessage = "Connect"
            };

            var envPath = Path.Combine(tempDir.FullName, ".env");
            var appSettingsPath = Path.Combine(tempDir.FullName, "appsettings.json");
            var processorDataPath = Path.Combine(tempDir.FullName, "ProcessorDataObj");
            var monitorIpsPath = Path.Combine(tempDir.FullName, "MonitorIPs");
            var legacyMonitorIpsPath = Path.Combine(tempDir.FullName, "MonitorIPS");
            File.WriteAllText(envPath, "AuthKey=old");
            File.WriteAllText(appSettingsPath, "{}");
            File.WriteAllText(processorDataPath, "state");
            File.WriteAllText(monitorIpsPath, "state");
            File.WriteAllText(legacyMonitorIpsPath, "state");

            var dialog = new TestDialogService { NextConfirmationResult = true };
            var dispatcher = new TestDispatcher();
            var platformService = new FakePlatformService { IsServiceStarted = true };

            var viewModel = new ConfigPageViewModel(
                GetLogger<ConfigPageViewModel>(),
                config,
                dialog,
                states,
                platformService,
                dispatcher,
                appDataDirectory: tempDir.FullName);

            await viewModel.ResetToDefaultsAsync();

            Assert.Equal(1, dialog.ConfirmationRequests);
            Assert.False(File.Exists(envPath));
            Assert.False(File.Exists(appSettingsPath));
            Assert.False(File.Exists(processorDataPath));
            Assert.False(File.Exists(monitorIpsPath));
            Assert.False(File.Exists(legacyMonitorIpsPath));
            Assert.Equal(string.Empty, config.AuthKey);
            Assert.Equal(string.Empty, config.RabbitPassword);
            Assert.False(config.AgentUserFlow.IsAuthorized);
            Assert.False(states.IsSetup);
            Assert.False(states.IsRunning);
            Assert.False(states.IsRabbitConnected);
            Assert.Equal(ConnectState.Error, states.IsConnectState);
            Assert.Contains("Close", states.SetupMessage);
            Assert.Contains("Reset complete", dialog.LastAlertTitle ?? string.Empty);
            Assert.False(platformService.IsServiceStarted);
        }
        finally
        {
            if (Directory.Exists(tempDir.FullName))
            {
                Directory.Delete(tempDir.FullName, true);
            }
        }
    }
}

public sealed class TestDialogService : IDialogService
{
    public int ConfirmationRequests { get; private set; }
    public bool NextConfirmationResult { get; set; } = true;
    public string? LastAlertTitle { get; private set; }
    public string? LastAlertMessage { get; private set; }

    public Task DisplayAlert(string title, string message, string cancel)
    {
        LastAlertTitle = title;
        LastAlertMessage = message;
        return Task.CompletedTask;
    }

    public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
    {
        ConfirmationRequests++;
        LastAlertTitle = title;
        LastAlertMessage = message;
        return Task.FromResult(NextConfirmationResult);
    }
}
