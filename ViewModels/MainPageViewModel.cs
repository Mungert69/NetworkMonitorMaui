using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using NetworkMonitor.Connection;
using System.Windows.Input;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;
using NetworkMonitor.Maui.Controls;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Processor.Services;

namespace NetworkMonitor.Maui.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly NetConnectConfig _netConfig;
        private readonly IPlatformService _platformService;
        private readonly ILogger _logger;
        private readonly IAuthService _authService;
        private CancellationTokenSource? _pollingCts;
        private bool _isServiceStarted;
        private bool _disableAgentOnServiceShutdown;
        private string _serviceMessage = "No Service Message";
        private string _authUrl;
        public string MonitorLocation => _netConfig?.MonitorLocation ?? "Unknown";
        private bool _isPolling;
        private bool _showTasks = false;
        private List<TaskItem> _tasks;
        private IColorResource ColorResource = ServiceInitializer.RootProvider.ColorResource;



        public bool ShowTasks
        {
            get => _showTasks;
            set
            {
                SetProperty(ref _showTasks, value);
            }
        }
        public string ServiceMessage
        {
            get => _serviceMessage;
            set => SetProperty(ref _serviceMessage, value);
        }
        public CancellationTokenSource? PollingCts { get => _pollingCts; set => _pollingCts = value; }


        public event EventHandler<(bool show, bool showCancel)> ShowLoadingMessage;
        public event EventHandler<(string Title, string Message)> ShowAlertRequested;
        public event EventHandler<string> OpenBrowserRequested;
        public event EventHandler<string> NavigateRequested;


        public List<TaskItem> Tasks => _tasks;

        public MainPageViewModel(NetConnectConfig netConfig, IPlatformService platformService, ILogger<MainPageViewModel> logger, IAuthService authService)
        {
            _netConfig = netConfig;
            _platformService = platformService;
            _logger = logger;
            _authService = authService;


            if (_platformService != null)
            {
                _isServiceStarted = _platformService?.IsServiceStarted ?? false;
                _disableAgentOnServiceShutdown = _platformService?.DisableAgentOnServiceShutdown ?? false;
                _serviceMessage = _platformService?.ServiceMessage ?? "The Agent is disabled";
            }
            else
            {
                _logger.LogError("_platformService is null in MainPageViewModel constructor.");
            }

            if (_netConfig?.AgentUserFlow != null)
            {
                _netConfig.AgentUserFlow.PropertyChanged += OnAgentUserFlowPropertyChanged;
            }
            else
            {
                _logger.LogError("_netConfig.AgentUserFlow is null in MainPageViewModel constructor.");
            }
            if (_netConfig.IsChatMode) _tasks=GetChatModeTasks();
            else _tasks = GetStandardModeTasks();

        }

        public List<TaskItem> GetTasks()
        {
            try
            {
                if (_netConfig.IsChatMode)
                {
                    return GetChatModeTasks();
                }
                else
                {
                    return GetStandardModeTasks();
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetupTasks : {e.Message}");
                return new List<TaskItem>
        {
            new TaskItem
            {
                TaskDescription = $"Failed to setup tasks : {e.Message}",
                IsCompleted = false,
                TaskAction = null
            }
        };
            }
        }

        private List<TaskItem> GetStandardModeTasks()
        {
            return new List<TaskItem>
    {
        new TaskItem
        {
            TaskDescription = "Authorize Agent",
            IsCompleted = _netConfig.AgentUserFlow.IsAuthorized,
            TaskAction = new Command(async () =>
            {
                try
                {
                    if (!_isPolling)
                    {
                        _isPolling = true;
                        await ExecuteAuthorizeAsync();
                        _isPolling = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing authorize action: {ex}");
                    _isPolling = false;
                }
            })
        },
        new TaskItem
        {
            TaskDescription = "Login Free Network Monitor",
            IsCompleted = _netConfig.AgentUserFlow.IsLoggedInWebsite,
            TaskAction = new Command(async () => await ExecuteLoginAsync())
        },
        new TaskItem
        {
            TaskDescription = "Scan for Hosts",
            IsCompleted = _netConfig.AgentUserFlow.IsHostsAdded,
            TaskAction = new Command(async () => await ExecuteScanHostsAsync())
        }
    };
        }

        private List<TaskItem> GetChatModeTasks()
        {
            return new List<TaskItem>
    {
        new TaskItem
        {
            TaskDescription = "Authorize Agent",
            IsCompleted = _netConfig.AgentUserFlow.IsAuthorized,
            TaskAction = new Command(async () =>
            {
                try
                {
                    if (!_isPolling)
                    {
                        _isPolling = true;
                        await ExecuteAuthorizeAsync();
                        _isPolling = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing authorize action: {ex}");
                    _isPolling = false;
                }
            })
        },
        new TaskItem
        {
            TaskDescription = "Open Network Monitor Assistant",
            IsCompleted =_netConfig.AgentUserFlow.IsChatOpened,
            TaskAction = new Command(async () => await ExecuteOpenAssistantAsync())
        }
    };
        }


        public async Task<bool> SetServiceStartedAsync(bool value)
        {
            try
            {
                // Trigger service state change
                await ChangeServiceAsync(value);

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
                ShowLoadingMessage?.Invoke(this, (true, false));
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
                    ShowLoadingMessage?.Invoke(this, (false, false));
                    MainThread.BeginInvokeOnMainThread(() =>
              {
                  ShowTasks = _isServiceStarted;
                  ServiceMessage = _platformService?.ServiceMessage ?? "";
              });


                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in ChangeServiceAsync Second Try Catch : {ex.Message}");
                }
            }
        }
        private void OnAgentUserFlowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_netConfig == null)
            {
                _logger.LogWarning("NetConnectConfig is null. Exiting OnAgentUserFlowPropertyChanged.");
                return;
            }

            if (_netConfig.AgentUserFlow == null)
            {
                _logger.LogWarning("AgentUserFlow is null in NetConnectConfig. Exiting OnAgentUserFlowPropertyChanged.");
                return;
            }
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(AgentUserFlow.IsAuthorized):
                            UpdateTaskCompletion("Authorize Agent", _netConfig.AgentUserFlow.IsAuthorized);
                            break;
                        case nameof(AgentUserFlow.IsLoggedInWebsite):
                            UpdateTaskCompletion("Login Free Network Monitor", _netConfig.AgentUserFlow.IsLoggedInWebsite);
                            break;
                        case nameof(AgentUserFlow.IsHostsAdded):
                            UpdateTaskCompletion("Scan for Hosts", _netConfig.AgentUserFlow.IsHostsAdded);
                            break;
                        case nameof(AgentUserFlow.IsChatOpened):
                            UpdateTaskCompletion("Network Monitor Assistant", _netConfig.AgentUserFlow.IsChatOpened);
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
            if (_tasks == null) return;

            try
            {
                var task = _tasks.FirstOrDefault(t => t.TaskDescription == taskDescription);
                if (task != null)
                {
                    task.IsCompleted = isCompleted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task completion for {taskDescription}: {ex.Message}");
            }
        }



        private async Task ExecuteAuthorizeAsync()
        {
            var result = await AuthorizeAsync();
            if (!result.Success)
            {
                // Raise an event to show an alert
                ShowAlertRequested?.Invoke(this, ("Error", result.Message));
                _isPolling = false;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_authUrl))
            {
                // If we need to open a browser, raise an event.
                OpenBrowserRequested?.Invoke(this, _authUrl);

                // Also, if you need to start polling in the background, do it here:
                await PollForTokenInBackgroundAsync();
            }
            else
            {
                ShowAlertRequested?.Invoke(this, ("Error", "Authorization URL is not available."));
                _logger.LogError("Authorization URL is not available");
                _isPolling = false;
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
        private async Task ExecuteOpenAssistantAsync()
        {
           var result = await OpenAssistantAsync();
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
            _isPolling = true;

            ShowLoadingMessage?.Invoke(this, (true, true));
            var result = await PollForTokenAsync(_pollingCts.Token);
            ShowLoadingMessage?.Invoke(this, (false, false));
            _isPolling = false;

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


        public async Task<ResultObj> AuthorizeAsync()
        {
            var resultInit = await _authService.InitializeAsync();
            if (!resultInit.Success)
                return resultInit;

            var resultSend = await _authService.SendAuthRequestAsync();
            if (!resultSend.Success)
                return resultSend;

            _authUrl = _netConfig.ClientAuthUrl;
            if (string.IsNullOrWhiteSpace(_authUrl))
            {
                return new ResultObj { Success = false, Message = "Authorization URL is not available." };
            }

            // If _authUrl is available, we can now poll for the token in background
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
            string pathStr = "//Scan";
# if Android
pathStr="Scan";
# endif
            // Return the navigation route
            return new ResultObj { Success = true, Message = pathStr };
        }

        private async Task<ResultObj> OpenAssistantAsync()
        {
            string pathStr = "//Chat";
#if Android
    pathStr = "Chat";
#endif
            return new ResultObj { Success = true, Message = pathStr };

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
        private IColorResource ColorResource = ServiceInitializer.RootProvider.ColorResource;

        public string ButtonText => _isCompleted ? $"{TaskDescription ?? "Task"} (Completed)" : TaskDescription ?? "Task";

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {

                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(ButtonText));
                    OnPropertyChanged(nameof(ButtonBackgroundColor));
                    OnPropertyChanged(nameof(ButtonTextColor));
                }

            }
        }

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
                            color = ColorResource.GetResourceColor("Gray950");
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
                Color color = Colors.Green;
                try
                {
                    if (_isCompleted)
                    {
                        color = ColorResource.GetResourceColor("Primary");
                    }
                    else
                    {
                        if (ColorResource.GetRequestedTheme() == AppTheme.Dark)
                        {
                            color = Colors.White;
                        }
                        else
                        {

                            color = Colors.Black;
                        }
                    }

                }
                catch { }
                return color;
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
