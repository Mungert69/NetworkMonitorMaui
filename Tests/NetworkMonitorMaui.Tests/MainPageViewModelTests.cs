using NetworkMonitor.Connection;
using System.Linq;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Objects;
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
}
