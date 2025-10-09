using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.ApplicationModel;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Logging;

namespace NetworkMonitor.Maui.ViewModels
{
    public class ExitPageViewModel : INotifyPropertyChanged
    {
        private readonly IPlatformService _platformService;
        private readonly ILogger _logger;
        private readonly IUiDispatcher _dispatcher;

        private bool _isBusy;
        private string _statusMessage;
        private TaskCompletionSource<bool>? _serviceStopCompletion;

        public ExitPageViewModel(
            IPlatformService platformService,
            ILogger<ExitPageViewModel> logger,
            IUiDispatcher? dispatcher = null)
        {
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            _logger = logger;
            _dispatcher = dispatcher ?? ServiceInitializer.Dispatcher;

            _platformService.ServiceStateChanged += HandleServiceStateChanged;

            _statusMessage = string.IsNullOrWhiteSpace(_platformService.ServiceMessage)
                ? "Choose how you would like to close the agent."
                : _platformService.ServiceMessage;

            ExitUiCommand = new Command(async () => await ExitAsync(disableAgent: false), () => !IsBusy);
            DisableAndExitCommand = new Command(async () => await ExitAsync(disableAgent: true), () => !IsBusy);
        }

        public ICommand ExitUiCommand { get; }
        public ICommand DisableAndExitCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    UpdateCommandState();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        private async Task ExitAsync(bool disableAgent)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                if (!disableAgent)
                {
                    StatusMessage = "Hiding agent UI and leaving service running...";
                    await _dispatcher.DispatchAsync(() =>
                    {
#if ANDROID
                        var activity = Platform.CurrentActivity;
                        if (activity != null)
                        {
                            activity.MoveTaskToBack(true);
                        }
                        else
                        {
                            Application.Current?.Quit();
                        }
#elif WINDOWS
                        Application.Current?.CloseWindow(Application.Current?.MainPage?.GetParentWindow());
#else
                        Application.Current?.CloseWindow(Application.Current?.MainPage?.GetParentWindow());
#endif
                        return Task.CompletedTask;
                    });
                    StatusMessage = "Agent continues running in the background.";
                    return;
                }

                if (_platformService.IsServiceStarted)
                {
                    StatusMessage = "Stopping agent...";
                    _serviceStopCompletion = new TaskCompletionSource<bool>();

                    await _platformService.ChangeServiceState(false).ConfigureAwait(false);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    try
                    {
                        await Task.WhenAny(_serviceStopCompletion.Task, Task.Delay(Timeout.Infinite, cts.Token)).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // ignore timeout; we'll proceed to exit anyway
                    }
                }

                StatusMessage = "Agent stopped. Closing application...";

                await _dispatcher.DispatchAsync(() =>
                {
#if ANDROID || WINDOWS
                    Application.Current?.Quit();
#else
                    Application.Current?.CloseWindow(Application.Current?.MainPage?.GetParentWindow());
#endif
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed while exiting application.");
                StatusMessage = $"Failed to exit: {ex.Message}";
            }
            finally
            {
                _serviceStopCompletion = null;
                IsBusy = false;
            }
        }

        private void HandleServiceStateChanged(object? sender, EventArgs e)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_platformService.ServiceMessage)
                ? (_platformService.IsServiceStarted ? "Started agent." : "Stopped agent.")
                : _platformService.ServiceMessage;

            if (_platformService.IsServiceStarted == false)
            {
                _serviceStopCompletion?.TrySetResult(true);
            }
        }

        private void UpdateCommandState()
        {
            if (ExitUiCommand is Command exitCommand)
            {
                exitCommand.ChangeCanExecute();
            }

            if (DisableAndExitCommand is Command disableCommand)
            {
                disableCommand.ChangeCanExecute();
            }
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
