using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.Factory;
using Xunit;

namespace NetworkMonitorMaui.Tests;

public class NetworkMonitorViewModelTests : ViewModelTestBase
{
    private static Task InvokeTestConnectionAsync(NetworkMonitorViewModel viewModel)
    {
        var method = typeof(NetworkMonitorViewModel)
            .GetMethod("TestConnection", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate TestConnection method via reflection.");

        if (method.Invoke(viewModel, Array.Empty<object>()) is not Task task)
        {
            throw new InvalidOperationException("TestConnection did not return a Task.");
        }

        return task;
    }

    [Fact]
    public async Task TestConnection_WithMissingInputs_ShowsValidationMessage()
    {
        var apiServiceMock = new Mock<IApiService>(MockBehavior.Strict);
        var viewModel = new NetworkMonitorViewModel(apiServiceMock.Object)
        {
            Address = string.Empty
        };

        viewModel.SelectedEndpointType = EndPointTypeFactory.GetFriendlyName("icmp");

        await InvokeTestConnectionAsync(viewModel);

        Assert.True(viewModel.HasResult);
        Assert.Equal("Please enter an address and select an endpoint type.", viewModel.ResultMessage);
        Assert.False(viewModel.IsBusy);
        Assert.Empty(apiServiceMock.Invocations);
    }

    [Fact]
    public async Task TestConnection_WithSuccessfulResponse_UpdatesResultProperties()
    {
        var apiServiceMock = new Mock<IApiService>();
        var resultPayload = new TResultObj<DataObj>
        {
            Success = true,
            Data = new DataObj
            {
                ResponseTime = 123,
                ResultStatus = "OK"
            }
        };

        apiServiceMock
            .Setup(s => s.CheckIcmp(It.IsAny<HostObject>()))
            .ReturnsAsync(resultPayload);

        var viewModel = new NetworkMonitorViewModel(apiServiceMock.Object)
        {
            Address = "example.com",
            Port = 42
        };

        var friendlyName = EndPointTypeFactory.GetFriendlyName("icmp");
        viewModel.SelectedEndpointType = friendlyName;

        await InvokeTestConnectionAsync(viewModel);

        apiServiceMock.Verify(
            s => s.CheckIcmp(It.Is<HostObject>(h => h.EndPointType == "icmp" && h.Address == viewModel.Address && h.Port == viewModel.Port)),
            Times.Once);

        Assert.True(viewModel.HasResult);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("Connection successful", viewModel.ResultMessage);
        Assert.Equal("123", viewModel.ResponseTime);
        Assert.Equal("OK", viewModel.ResultStatus);
    }

    [Fact]
    public async Task TestConnection_WithFailedResponse_SetsFailureMessage()
    {
        var apiServiceMock = new Mock<IApiService>();
        apiServiceMock
            .Setup(s => s.CheckIcmp(It.IsAny<HostObject>()))
            .ReturnsAsync(new TResultObj<DataObj> { Success = false, Data = new DataObj() });

        var viewModel = new NetworkMonitorViewModel(apiServiceMock.Object)
        {
            Address = "example.com",
            Port = 80
        };

        viewModel.SelectedEndpointType = EndPointTypeFactory.GetFriendlyName("icmp");

        await InvokeTestConnectionAsync(viewModel);

        Assert.True(viewModel.HasResult);
        Assert.Equal("Connection failed", viewModel.ResultMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task TestConnection_WhenApiThrows_ShowsError()
    {
        var apiServiceMock = new Mock<IApiService>();
        apiServiceMock
            .Setup(s => s.CheckIcmp(It.IsAny<HostObject>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var viewModel = new NetworkMonitorViewModel(apiServiceMock.Object)
        {
            Address = "example.com",
            Port = 80
        };

        viewModel.SelectedEndpointType = EndPointTypeFactory.GetFriendlyName("icmp");

        await InvokeTestConnectionAsync(viewModel);

        Assert.True(viewModel.HasResult);
        Assert.Contains("boom", viewModel.ResultMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public void TestConnectionCommand_ExecutesTestConnection()
    {
        var tcs = new TaskCompletionSource<TResultObj<DataObj>>();
        var apiServiceMock = new Mock<IApiService>();
        apiServiceMock
            .Setup(s => s.CheckIcmp(It.IsAny<HostObject>()))
            .Returns(tcs.Task);

        var viewModel = new NetworkMonitorViewModel(apiServiceMock.Object)
        {
            Address = "example.com",
            Port = 80
        };

        viewModel.SelectedEndpointType = EndPointTypeFactory.GetFriendlyName("icmp");

        viewModel.TestConnectionCommand.Execute(null);

        Assert.True(viewModel.IsBusy);

        tcs.SetResult(new TResultObj<DataObj> { Success = true });

        Assert.True(SpinWait.SpinUntil(() => !viewModel.IsBusy, TimeSpan.FromMilliseconds(250)));
        Assert.True(viewModel.HasResult);
    }
}
