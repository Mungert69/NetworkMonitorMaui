using System.Collections.Generic;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Objects;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class ProcessorStatesViewModelTests : ViewModelTestBase
{
    private static ProcessorStatesViewModel CreateViewModel(LocalProcessorStates states)
    {
        return new ProcessorStatesViewModel(GetLogger<ProcessorStatesViewModel>(), states);
    }

    [Fact]
    public void ConstructorCopiesInitialState()
    {
        var states = new LocalProcessorStates
        {
            IsRunning = true,
            IsSetup = true,
            IsConnectState = ConnectState.Running,
            IsRabbitConnected = true,
            SetupMessage = "Setup",
            RabbitSetupMessage = "Rabbit",
            RunningMessage = "Running",
            ConnectRunningMessage = "Connect"
        };

        var viewModel = CreateViewModel(states);

        Assert.True(viewModel.IsRunning);
        Assert.Equal("Setup", viewModel.SetupMessage);
        Assert.Equal("Connect", viewModel.ConnectRunningMessage);
    }

    [Fact]
    public void ShowPopupCommandDisplaysRequestedMessage()
    {
        var states = new LocalProcessorStates
        {
            RunningMessage = "Running",
            ConnectRunningMessage = "Connect",
            SetupMessage = "Setup",
            RabbitSetupMessage = "Rabbit"
        };

        var viewModel = CreateViewModel(states);

        viewModel.ShowPopupCommand.Execute("RunningMessage");

        Assert.True(viewModel.IsPopupVisible);
        Assert.Contains("Running Message", viewModel.PopupMessage);
    }

    [Fact]
    public void PropertyChange_UpdatesBindingsAndPopup()
    {
        var states = new LocalProcessorStates
        {
            RunningMessage = "Initial"
        };
        var dispatcher = new TestDispatcher();
        var viewModel = new ProcessorStatesViewModel(GetLogger<ProcessorStatesViewModel>(), states, dispatcher);

        var observedProperties = new List<string>();
        viewModel.PropertyChanged += (_, args) => observedProperties.Add(args.PropertyName ?? string.Empty);

        viewModel.ShowPopupCommand.Execute("RunningMessage");
        states.RunningMessage = "Updated";

        Assert.Contains(nameof(ProcessorStatesViewModel.RunningMessage), observedProperties);
        Assert.Contains("Updated", viewModel.PopupMessage);
        Assert.True(viewModel.IsPopupVisible);
        Assert.True(dispatcher.DispatchCalls > 0);
    }
}
