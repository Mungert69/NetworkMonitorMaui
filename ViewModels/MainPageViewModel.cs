using System;
using System.Collections.Generic;
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
        private readonly IDeviceContextService? _deviceContextService;
        private readonly IUiDispatcher _dispatcher;
        private CancellationTokenSource? _pollingCts;
        private bool _isServiceStarted;
        private bool _disableAgentOnServiceShutdown;
        private string _serviceMessage = "No Service Message";
        private string _authUrl;
        public string MonitorLocation => _netConfig?.MonitorLocation ?? "Unknown";
        private bool _isPolling;
        private bool _showTasks = false;
        private List<TaskItem> _tasks;
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

        // Add optional dispatcher parameter and follow ExitPageViewModel pattern
        public MainPageViewModel(
            NetConnectConfig netConfig,
            IPlatformService platformService,
            ILogger<MainPageViewModel> logger,
            IAuthService authService,
            IDeviceContextService? deviceContextService = null,
            IUiDispatcher? dispatcher = null)
        {
            _netConfig = netConfig;
            _platformService = platformService;
            _logger = logger;
            _authService = authService;
            _deviceContextService = deviceContextService;
            _dispatcher = dispatcher ?? ServiceInitializer.Dispatcher;

            if (_platformService != null)
            {
                _platformService.ServiceStateChanged += OnPlatformServiceStateChanged;
                _disableAgentOnServiceShutdown = _platformService.DisableAgentOnServiceShutdown;
                UpdateFromPlatformService();
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
            if (_netConfig != null && _netConfig.IsChatMode) _tasks = GetChatModeTasks();
            else _tasks = GetStandardModeTasks();
            ApplyAgentUserFlowToTasks();

        }

        private void OnPlatformServiceStateChanged(object? sender, EventArgs e)
        {
            // Use dispatcher to marshal to UI thread
            try
            {
                _dispatcher.Dispatch(UpdateFromPlatformService);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching platform service state change");
            }
        }

        private void UpdateFromPlatformService()
        {
            if (_platformService == null)
            {
                return;
            }

            IsServiceStarted = _platformService.IsServiceStarted;
            _disableAgentOnServiceShutdown = _platformService.DisableAgentOnServiceShutdown;
            ServiceMessage = string.IsNullOrWhiteSpace(_platformService.ServiceMessage)
                ? "The Agent is disabled"
                : _platformService.ServiceMessage;
            ShowTasks = IsServiceStarted;
            if (IsServiceStarted)
            {
                ApplyAgentUserFlowToTasks();
            }
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
                TaskAction = new Microsoft.Maui.Controls.Command(() => { })
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
            TaskDescription = "Login Quantum Network Monitor",
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
            TaskDescription = "Login Quantum Network Monitor",
            IsCompleted = _netConfig.AgentUserFlow.IsLoggedInWebsite,
            TaskAction = new Command(async () => await ExecuteLoginAsync())
        },
        new TaskItem
        {
            TaskDescription = "Open Monitor Assistant",
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

                if (value && IsServiceStarted && _deviceContextService != null)
                {
                    _ = RefreshDeviceContextAfterAgentStartAsync();
                }

                return IsServiceStarted; // Return actual service state
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing service state: {ex.Message}");
                return false; // Indicate failure
            }
        }

        private async Task RefreshDeviceContextAfterAgentStartAsync()
        {
            try
            {
                await _deviceContextService!.RefreshAndPersistAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Device context refresh after agent start failed.");
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
                    UpdateFromPlatformService();
                    ShowLoadingMessage?.Invoke(this, (false, false));


                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in ChangeServiceAsync Second Try Catch : {ex.Message}");
                }
            }
        }

        public bool IsServiceStarted
        {
            get => _isServiceStarted;
            private set => SetProperty(ref _isServiceStarted, value);
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
                // Use dispatcher to marshal UI updates
                _dispatcher.Dispatch(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(AgentUserFlow.IsAuthorized):
                            UpdateTaskCompletion("Authorize Agent", _netConfig.AgentUserFlow.IsAuthorized);
                            break;
                        case nameof(AgentUserFlow.IsLoggedInWebsite):
                            UpdateTaskCompletion("Login Quantum Network Monitor", _netConfig.AgentUserFlow.IsLoggedInWebsite);
                            break;
                        case nameof(AgentUserFlow.IsHostsAdded):
                            UpdateTaskCompletion("Scan for Hosts", _netConfig.AgentUserFlow.IsHostsAdded);
                            break;
                        case nameof(AgentUserFlow.IsChatOpened):
                            UpdateTaskCompletion("Open Monitor Assistant", _netConfig.AgentUserFlow.IsChatOpened);
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

        private void ApplyAgentUserFlowToTasks()
        {
            if (_tasks == null || _netConfig?.AgentUserFlow == null)
            {
                return;
            }

            try
            {
                UpdateTaskCompletion("Authorize Agent", _netConfig.AgentUserFlow.IsAuthorized);
                UpdateTaskCompletion("Login Quantum Network Monitor", _netConfig.AgentUserFlow.IsLoggedInWebsite);

                if (_netConfig.IsChatMode)
                {
                    UpdateTaskCompletion("Open Monitor Assistant", _netConfig.AgentUserFlow.IsChatOpened);
                }
                else
                {
                    UpdateTaskCompletion("Scan for Hosts", _netConfig.AgentUserFlow.IsHostsAdded);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying AgentUserFlow state to tasks");
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
                _netConfig.AgentUserFlow.IsLoggedInWebsite = true;
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
            _pollingCts ??= new CancellationTokenSource();
            var result = await PollForTokenAsync(_pollingCts.Token);
            ShowLoadingMessage?.Invoke(this, (false, false));
            _isPolling = false;

            if (result.Success)
            {
                _netConfig.AgentUserFlow.IsAuthorized = true;
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
            return new ResultObj { Success = true, Message = $"{AppConstants.FrontendUrl}/dashboard" };
        }

        public async Task<ResultObj> ScanHostsAsync()
        {
            string pathStr = "//Scan";
            // Return the navigation route
            return new ResultObj { Success = true, Message = pathStr };
        }

        private async Task<ResultObj> OpenAssistantAsync()
        {
            string pathStr = "//Chat";
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

        public async Task SomeMethod()
        {
            await Task.CompletedTask;
            // ...existing code...
        }
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;
        public string TaskDescription { get; set; } = "";
        private readonly IColorResource _colorResource;

        public TaskItem(IColorResource? colorResource = null)
        {
            if (colorResource != null)
            {
                _colorResource = colorResource;
                return;
            }

            try
            {
                _colorResource = ServiceInitializer.RootProvider.ColorResource;
            }
            catch (InvalidOperationException)
            {
                _colorResource = new FallbackColorResource();
            }
        }

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
                        if (_colorResource.GetRequestedTheme() == AppTheme.Dark)
                        {
                            color = _colorResource.GetResourceColor("Gray950");
                        }
                        else
                        {
                            color = Colors.White;
                        }
                    }
                    else
                    {
                        color = _colorResource.GetResourceColor("Warning");
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
                        color = _colorResource.GetResourceColor("Primary");
                    }
                    else
                    {
                        if (_colorResource.GetRequestedTheme() == AppTheme.Dark)
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

        private sealed class FallbackColorResource : IColorResource
        {
            private readonly Dictionary<string, Color> _colors = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Warning"] = Colors.Yellow,
                ["Primary"] = Colors.Blue,
                ["Gray950"] = Colors.Black
            };

            public AppTheme GetRequestedTheme() => AppTheme.Light;

            public Color GetResourceColor(string key) =>
                _colors.TryGetValue(key, out var color) ? color : Colors.White;

            public Color LightenColor(Color color, float factor)
            {
                factor = Math.Max(0, factor);
                return new Color(
                    (float)Math.Min(color.Red + factor, 1.0f),
                    (float)Math.Min(color.Green + factor, 1.0f),
                    (float)Math.Min(color.Blue + factor, 1.0f),
                    (float)color.Alpha);
            }

            public void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length)
            {
                boxView.Color = toColor;
            }
        }
    }
}
