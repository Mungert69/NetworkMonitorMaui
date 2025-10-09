using Microsoft.Maui.Controls;
using NetworkMonitor.Maui.ViewModels;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class EventToCommandBehaviorTests
{
    [Fact]
    public void CommandExecutesWhenSwitchToggles()
    {
        var behavior = new EventToCommandBehavior();
        bool executed = false;
        behavior.Command = new Command<bool>(_ => executed = true);

        var toggle = new Switch();
        toggle.Behaviors.Add(behavior);

        toggle.IsToggled = true;

        Assert.True(executed);
    }
}
