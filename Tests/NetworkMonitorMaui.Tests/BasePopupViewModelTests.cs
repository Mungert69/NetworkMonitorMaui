using System.Collections.Generic;
using NetworkMonitor.Maui.ViewModels;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class BasePopupViewModelTests
{
    [Fact]
    public void ClosePopupCommand_HidesPopup()
    {
        var viewModel = new BasePopupViewModel
        {
            IsPopupVisible = true
        };

        viewModel.ClosePopupCommand.Execute(null);

        Assert.False(viewModel.IsPopupVisible);
    }

    [Fact]
    public void SettingPopupMessage_RaisesPropertyChanged()
    {
        var viewModel = new BasePopupViewModel();
        var observedProperties = new List<string>();
        viewModel.PropertyChanged += (_, args) => observedProperties.Add(args.PropertyName ?? string.Empty);

        viewModel.PopupMessage = "Hello";

        Assert.Contains(nameof(BasePopupViewModel.PopupMessage), observedProperties);
    }
}
