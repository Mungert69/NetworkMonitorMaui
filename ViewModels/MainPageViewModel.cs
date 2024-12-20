using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using NetworkMonitor.Connection;
using System.Windows.Input;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Processor.Services;
using OpenAI.ObjectModels.ResponseModels;

namespace NetworkMonitor.Maui.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private NetConnectConfig _netConfig;
        private NetworkMonitor.Maui.Services.IPlatformService _platformService;
        private ILogger _logger;
        private IAuthService _authService;  // Added
        private CancellationTokenSource? _pollingCts;

        public ICommand ToggleServiceCommand { get; }
        public event EventHandler<(bool show ,bool showCancel)> ShowLoadingMessage;
        public event EventHandler<(string Title, string Message)> ShowAlertRequested;
        public event EventHandler<string> OpenBrowserRequested;
        public event EventHandler<string> NavigateRequested;


        public ObservableCollection<TaskItem> Tasks { get; set; } = new ObservableCollection<TaskItem>();

 public MainPageViewModel(NetConnectConfig netConfig, IPlatformService platformService, ILogger logger, IAuthService authService)
        {
            _netConfig = netConfig;
            _platformService = platformService;
            _logger = logger;
            _authService = authService;

            if (_platformService != null)
            {
               // _platformService.ServiceStateChanged += PlatformServiceStateChanged;
                // Initialize local fields based on current platform state
                _isServiceStarted = _platformService.IsServiceStarted;
                _disableAgentOnServiceShutdown = _platformService.DisableAgentOnServiceShutdown;
                _serviceMessage = _platformService.ServiceMessage ?? "No Service Message";
            }
            else
            {
                _logger.LogError("_platformService is null in MainPageViewModel constructor.");
            }

            if (_netConfig?.AgentUserFlow != null)
            {
                _netConfig.AgentUserFlow.PropertyChanged += OnAgentUserFlowPropertyChanged;
                _agentUserFlow = _netConfig.AgentUserFlow;
            }
            else
            {
                _logger.LogError("_netConfig.AgentUserFlow is null in MainPageViewModel constructor.");
            }
            SetupTasks();
            ToggleServiceCommand = new Command<bool>(async (value) => await SetServiceStartedAsync(value));
        }

       

        // Property to hold the authorization URL previously handled in MainPage
        private string _authUrl;
        public string AuthUrl
        {
            get => _authUrl;
            private set => SetProperty(ref _authUrl, value);
        }

        // Expose the MonitorLocation so MainPage can display it if needed
        public string MonitorLocation => _netConfig?.MonitorLocation ?? "Unknown";

        private bool _isPolling;
        public bool IsPolling
        {
            get => _isPolling;
            set => SetProperty(ref _isPolling, value);
        }

        private bool _showToggle = true;
        public bool ShowToggle
        {
            get => _showToggle;
            set
            {
                SetProperty(ref _showToggle, value);
            }
        }

          private bool _showTasks = false;
        public bool ShowTasks
        {
            get => _showTasks;
            set
            {
                SetProperty(ref _showTasks, value);
            }
        }


        // Fields that mirror platform service properties
        private bool _isServiceStarted;

        private bool _disableAgentOnServiceShutdown;
        private string _serviceMessage = "No Service Message";
        private AgentUserFlow _agentUserFlow;

       

        public string ServiceMessage
        {
            get => _serviceMessage;
            set => SetProperty(ref _serviceMessage, value);
        }
        public CancellationTokenSource? PollingCts { get => _pollingCts; set => _pollingCts = value; }

     
public async Task<bool> SetServiceStartedAsync(bool value)
{
    try
    {
        // Trigger service state change
        await ChangeServiceAsync(value);

        // Update the toggle visibility and return the final state
        if (_isServiceStarted && !value && _disableAgentOnServiceShutdown)
        {
            ShowToggle = false;
        }

        return _isServiceStarted; // Return actual service state
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error changing service state: {ex.Message}");
        return false; // Indicate failure
    }
}

        private async Task ChangeServiceAsync(bool state)
        {
            try
            {
                ShowToggle = false;
                ShowLoadingMessage?.Invoke(this, (true,false));
                await Task.Delay(200);
                await _platformService.ChangeServiceState(state);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in ChangeServiceAsync First Try Catch : {e.Message}");
            }
            finally
            {
                try
                {
                    _isServiceStarted = _platformService?.IsServiceStarted ?? false;
                    _disableAgentOnServiceShutdown = _platformService?.DisableAgentOnServiceShutdown ?? false;
                    ServiceMessage = _platformService?.ServiceMessage ?? "No Service Message";

                    ShowLoadingMessage?.Invoke(this, (false,false));
                    ShowTasks=_platformService?.IsServiceStarted ?? false;
                    ShowToggle = true;

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in ChangeServiceAsync Second Try Catch : {ex.Message}");
                }
            }
        }
        private void OnAgentUserFlowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(AgentUserFlow.IsAuthorized):
                            _agentUserFlow.IsAuthorized = _netConfig.AgentUserFlow.IsAuthorized;
                            UpdateTaskCompletion("Authorize Agent", _agentUserFlow.IsAuthorized);
                            break;
                        case nameof(AgentUserFlow.IsLoggedInWebsite):
                            _agentUserFlow.IsLoggedInWebsite = _netConfig.AgentUserFlow.IsLoggedInWebsite;
                            UpdateTaskCompletion("Login Free Network Monitor", _agentUserFlow.IsLoggedInWebsite);
                            break;
                        case nameof(AgentUserFlow.IsHostsAdded):
                            _agentUserFlow.IsHostsAdded = _netConfig.AgentUserFlow.IsHostsAdded;
                            UpdateTaskCompletion("Scan for Hosts", _agentUserFlow.IsHostsAdded);
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnAgentUserFlowPropertyChanged : {ex.Message}");
            }
        }

        public void UpdateTaskCompletion(string taskDescription, bool isCompleted)
        {
            try
            {
                var task = Tasks.FirstOrDefault(t => t.TaskDescription == taskDescription);
                if (task != null)
                {
                    task.IsCompleted = isCompleted;
                    OnPropertyChanged(nameof(Tasks));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task completion for {taskDescription}: {ex.Message}");
            }
        }


        public void SetupTasks()
        {
            try
            {
                Tasks = new ObservableCollection<TaskItem>
        {
            new TaskItem
            {
                TaskDescription = "Authorize Agent",
                IsCompleted = _agentUserFlow.IsAuthorized,
                TaskAction = new Command(async () =>
                {
                    try
                    {
                        if (!IsPolling)
                        {
                            IsPolling = true;
                            await ExecuteAuthorizeAsync();
                            IsPolling = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error executing authorize action: {ex}");
                        IsPolling = false; // Ensure we reset the flag even if there's an error
                    }
                })
            },
            new TaskItem
            {
                TaskDescription = "Login Free Network Monitor",
                IsCompleted = _agentUserFlow.IsLoggedInWebsite,
                TaskAction = new Command(async () => await ExecuteLoginAsync())
            },
            new TaskItem
            {
                TaskDescription = "Scan for Hosts",
                IsCompleted = _agentUserFlow.IsHostsAdded,
                TaskAction = new Command(async () => await ExecuteScanHostsAsync())
            }
        };
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetupTasks : {e.Message}");
            }
        }

        private async Task ExecuteAuthorizeAsync()
        {
            var result = await AuthorizeAsync();
            if (!result.Success)
            {
                // Raise an event to show an alert
                ShowAlertRequested?.Invoke(this, ("Error", result.Message));
                IsPolling = false;
                return;
            }

            if (!string.IsNullOrWhiteSpace(AuthUrl))
            {
                // If we need to open a browser, raise an event.
                OpenBrowserRequested?.Invoke(this, AuthUrl);

                // Also, if you need to start polling in the background, do it here:
                await PollForTokenInBackgroundAsync();
            }
            else
            {
                ShowAlertRequested?.Invoke(this, ("Error", "Authorization URL is not available."));
                _logger.LogError("Authorization URL is not available");
                IsPolling = false;
            }
        }

        private async Task ExecuteLoginAsync()
        {
            var result = await OpenLoginWebsiteAsync();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Message))
            {
                OpenBrowserRequested?.Invoke(this, result.Message);
            }
            else
            {
                ShowAlertRequested?.Invoke(this, ("Error", "Login URL is not available."));
                _logger.LogError("Login URL is not available");
            }
        }

        private async Task ExecuteScanHostsAsync()
        {
            var result = await ScanHostsAsync();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Message))
            {
                NavigateRequested?.Invoke(this, result.Message);
            }
            else
            {
                ShowAlertRequested?.Invoke(this, ("Error", "Navigation URL is not available."));
                _logger.LogError("Navigation URL is not available");
            }
        }

        private async Task PollForTokenInBackgroundAsync()
        {
            IsPolling = true;
            
            ShowLoadingMessage?.Invoke(this, (true,true));
            var result = await PollForTokenAsync(_pollingCts.Token);
            ShowLoadingMessage?.Invoke(this, (false,false));
            IsPolling = false;

            if (result.Success)
            {
                ShowAlertRequested?.Invoke(this, ("Success", $"Authorization successful! Now login and add hosts using '{MonitorLocation}' as the monitor location."));
            }
            else
            {
                ShowAlertRequested?.Invoke(this, ("Fail", result.Message));
                _logger.LogError($"PollForToken failed: {result.Message}");
            }
        }



        // New Methods that encapsulate logic originally in MainPage:

        public async Task<ResultObj> AuthorizeAsync()
        {
            var resultInit = await _authService.InitializeAsync();
            if (!resultInit.Success)
                return resultInit;

            var resultSend = await _authService.SendAuthRequestAsync();
            if (!resultSend.Success)
                return resultSend;

            AuthUrl = _netConfig.ClientAuthUrl;
            if (string.IsNullOrWhiteSpace(AuthUrl))
            {
                return new ResultObj { Success = false, Message = "Authorization URL is not available." };
            }

            // If AuthUrl is available, we can now poll for the token in background
            return new ResultObj { Success = true, Message = "Authorized successfully." };
        }

        public async Task<ResultObj> PollForTokenAsync(CancellationToken token)
        {
            var pollResult = await _authService.PollForTokenAsync(token);
            return pollResult;
        }

        public async Task<ResultObj> OpenLoginWebsiteAsync()
        {
            // Just return a successful result along with the URL
            return new ResultObj { Success = true, Message = "https://freenetworkmonitor.click/dashboard" };
        }

        public async Task<ResultObj> ScanHostsAsync()
        {
            // Return the navigation route
            return new ResultObj { Success = true, Message = "//Scan" };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            try
            {
                if (Equals(storage, value))
                {
                    return false;
                }

                storage = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetProperty: {e.Message}");
                return false;
            }
        }
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;
        public string TaskDescription { get; set; } = "";
        public string ButtonText => IsCompleted ? $"{TaskDescription ?? "Task"} (Completed)" : TaskDescription ?? "Task";
        public Color ButtonBackgroundColor
        {
            get
            {
                Color color = Colors.White;
                try
                {

                    if (_isCompleted)
                    {
                        if (ColorResource.GetRequestedTheme() == AppTheme.Dark)
                        {
                            color = ColorResource.GetResourceColor("Grey950");
                        }
                        else
                        {
                            color = Colors.White;
                        }
                    }
                    else
                    {
                        color = ColorResource.GetResourceColor("Warning");
                    }

                    return color;
                }
                catch
                {
                    return color;
                }
            }
        }

        public Color ButtonTextColor
        {
            get
            {
                try
                {
                    if (_isCompleted)
                    {
                        return ColorResource.GetResourceColor("Primary");
                    }
                    else { return Colors.White; }
                }
                catch
                {
                    return Colors.White;
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    // If needed, ensure we're on the main thread before firing PropertyChanged
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ButtonText));
                        OnPropertyChanged(nameof(ButtonBackgroundColor));
                        OnPropertyChanged(nameof(ButtonTextColor));
                    });
                }
            }
        }

        public ICommand TaskAction { get; set; }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
