using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Connection;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Objects;
using NetworkMonitor.Processor.Services;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class MainPageViewModelTests : ViewModelTestBase
{
    private static MainPageViewModel CreateViewModel(
        NetConnectConfig? config = null,
        FakePlatformService? platformService = null,
        FakeAuthService? authService = null)
    {
        config ??= CreateNetConnectConfig();
        platformService ??= new FakePlatformService { ServiceMessage = "Agent idle" };
        authService ??= new FakeAuthService();
        return new MainPageViewModel(config, platformService, GetLogger<MainPageViewModel>(), authService);
    }

    [Fact]
    public void GetTasks_StandardModeReflectsAgentFlow()
    {
        var config = CreateNetConnectConfig();
        config.IsChatMode = false;
        config.AgentUserFlow.IsAuthorized = true;
        config.AgentUserFlow.IsLoggedInWebsite = false;
        config.AgentUserFlow.IsHostsAdded = true;

        var viewModel = CreateViewModel(config);

        var tasks = viewModel.GetTasks();

        Assert.Collection(tasks,
            t => { Assert.Equal("Authorize Agent", t.TaskDescription); Assert.True(t.IsCompleted); },
            t => { Assert.Equal("Login Quantum Network Monitor", t.TaskDescription); Assert.False(t.IsCompleted); },
            t => { Assert.Equal("Scan for Hosts", t.TaskDescription); Assert.True(t.IsCompleted); });
    }

    [Fact]
    public void GetTasks_ChatModeIncludesAssistantTask()
    {
        var config = CreateNetConnectConfig();
        config.IsChatMode = true;
        config.AgentUserFlow.IsAuthorized = true;
        config.AgentUserFlow.IsLoggedInWebsite = true;
        config.AgentUserFlow.IsChatOpened = false;

        var viewModel = CreateViewModel(config);

        var tasks = viewModel.GetTasks();

        Assert.Contains(tasks, t => t.TaskDescription == "Open Monitor Assistant");
    }

    [Fact]
    public async Task AuthorizeAsync_SuccessReturnsExpectedMessage()
    {
        var config = CreateNetConnectConfig();
        config.ClientAuthUrl = "https://auth.example.com/device";
        var authService = new FakeAuthService
        {
            InitializeResult = new ResultObj { Success = true },
            SendResult = new ResultObj { Success = true }
        };

        var viewModel = CreateViewModel(config, authService: authService);

        var result = await viewModel.AuthorizeAsync();

        Assert.True(result.Success);
        Assert.Equal("Authorized successfully.", result.Message);
        Assert.Equal(1, authService.InitializeCalls);
        Assert.Equal(1, authService.SendAuthCalls);
    }

    [Fact]
    public void UpdateTaskCompletion_TogglesTaskState()
    {
        var config = CreateNetConnectConfig();
        var viewModel = CreateViewModel(config);
        var task = viewModel.Tasks.First(t => t.TaskDescription == "Authorize Agent");

        viewModel.UpdateTaskCompletion("Authorize Agent", true);

        Assert.True(viewModel.Tasks.First(t => t.TaskDescription == "Authorize Agent").IsCompleted);
        Assert.Contains("Completed", task.ButtonText);
    }

    [Fact]
    public async Task ExecuteAuthorizeAsync_RaisesBrowserAndSuccessAlert()
    {
        var config = CreateNetConnectConfig();
        config.ClientAuthUrl = "https://auth.example.com/device";
        var platformService = new FakePlatformService { ServiceMessage = "Idle" };
        var authService = new FakeAuthService
        {
            InitializeResult = new ResultObj { Success = true, Message = "init" },
            SendResult = new ResultObj { Success = true, Message = "send" },
            PollResult = new ResultObj { Success = true, Message = "authorized" }
        };

        var viewModel = CreateViewModel(config, platformService, authService);
        viewModel.PollingCts = new CancellationTokenSource();

        var browserRequests = new List<string>();
        var loadingStates = new List<(bool show, bool showCancel)>();
        var alerts = new List<(string Title, string Message)>();

        viewModel.OpenBrowserRequested += (_, url) => browserRequests.Add(url);
        viewModel.ShowLoadingMessage += (_, args) => loadingStates.Add(args);
        viewModel.ShowAlertRequested += (_, args) => alerts.Add(args);

        var method = typeof(MainPageViewModel)
            .GetMethod("ExecuteAuthorizeAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ExecuteAuthorizeAsync not found.");

        await ((Task)method.Invoke(viewModel, Array.Empty<object>())!);

        Assert.Contains(config.ClientAuthUrl, browserRequests);
        Assert.Contains(loadingStates, state => state.show && state.showCancel);
        Assert.Contains(loadingStates, state => !state.show);
        Assert.Contains(alerts, alert => alert.Title == "Success" && alert.Message.Contains("Authorization successful"));
        Assert.Equal(1, authService.InitializeCalls);
        Assert.Equal(1, authService.SendAuthCalls);
        Assert.Equal(1, authService.PollCalls);
    }
}
