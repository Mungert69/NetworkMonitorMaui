using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NetworkMonitor.Connection;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace NetworkMonitor.Maui.ViewModels
{
    public class ConfigPageViewModel : INotifyPropertyChanged
    {
        private readonly NetConnectConfig _netConfig;
        private readonly ILogger _logger;
        private readonly IUiDispatcher _dispatcher;
        private readonly IDialogService _dialogService;
        private readonly IPlatformService _platformService;
        private readonly LocalProcessorStates _processorStates;
        private readonly string _appDataDirectory;
        private bool _isResetting;

        public ConfigPageViewModel(
            ILogger<ConfigPageViewModel> logger,
            NetConnectConfig netConfig,
            IDialogService dialogService,
            LocalProcessorStates processorStates,
            IPlatformService platformService,
            IUiDispatcher? dispatcher = null,
            string? appDataDirectory = null)
        {
            try {
                _logger = logger;
                _netConfig = netConfig;
                _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
                _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
                _processorStates = processorStates ?? throw new ArgumentNullException(nameof(processorStates));
                _dispatcher = dispatcher ?? ServiceInitializer.Dispatcher;
                _appDataDirectory = ResolveAppDataDirectory(appDataDirectory);
                _netConfig.PropertyChanged += NetConfig_PropertyChanged;
                ResetCommand = new Command(async () => await ResetToDefaultsAsync(), () => !_isResetting);
                  }
            catch (Exception ex)
            {
                _logger?.LogError($" Error : initializing ConfigPageViewModel. Error was: {ex}");
            }

        }

        public ICommand ResetCommand { get; }

        internal async Task ResetToDefaultsAsync()
        {
            if (_isResetting)
            {
                return;
            }

            _isResetting = true;
            UpdateResetCommandCanExecute();

            try
            {
                var confirmed = await _dialogService.DisplayAlert(
                    "Reset agent configuration?",
                    "This will delete appsettings.json and .env from the agent storage and disable the agent until you close and reopen the app. Do you want to continue?",
                    "Reset",
                    "Cancel");

                if (!confirmed)
                {
                    return;
                }

                var envPath = Path.Combine(_appDataDirectory, ".env");
                var appSettingsPath = Path.Combine(_appDataDirectory, "appsettings.json");

                var warnings = new List<string>();

                if (!TryDeleteFile(envPath, warnings, ".env"))
                {
                    _logger.LogWarning("Unable to delete environment file during reset: {Path}", envPath);
                }

                if (!TryDeleteFile(appSettingsPath, warnings, "appsettings.json"))
                {
                    _logger.LogWarning("Unable to delete appsettings file during reset: {Path}", appSettingsPath);
                }

                try
                {
                    _platformService.DisableAgentOnServiceShutdown = true;
                    await _platformService.ChangeServiceState(false).ConfigureAwait(false);
                    _platformService.OnUpdateServiceState(
                        new ResultObj
                        {
                            Success = true,
                            Message = "Reset complete. Agent disabled. Close the app to finish."
                        },
                        false);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Agent service: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to stop agent service during reset.");
                }

                Environment.SetEnvironmentVariable("AuthKey", null);
                Environment.SetEnvironmentVariable("RabbitPassword", null);

                _netConfig.AuthKey = string.Empty;
                _netConfig.RabbitPassword = string.Empty;
                _netConfig.AgentUserFlow.IsAuthorized = false;
                _netConfig.AgentUserFlow.IsLoggedInWebsite = false;
                _netConfig.AgentUserFlow.IsHostsAdded = false;
                _netConfig.AgentUserFlow.IsChatOpened = false;

                _processorStates.IsSetup = false;
                _processorStates.IsRunning = false;
                _processorStates.IsConnectRunning = false;
                _processorStates.IsRabbitConnected = false;
                _processorStates.IsConnectState = ConnectState.Error;
                _processorStates.SetupMessage = "Agent reset to factory defaults. Tap Close, exit the app, and reopen to finish.";
                _processorStates.RunningMessage = "Agent disabled after reset.";
                _processorStates.ConnectRunningMessage = "Monitoring paused until the app is restarted.";
                _processorStates.RabbitSetupMessage = "RabbitMQ credentials cleared; please restart the app.";

                var message = warnings.Count == 0
                    ? "Configuration reset completed. Please tap Close to exit the app, then reopen it to finish the reset."
                    : $"Configuration reset completed with warnings:\n - {string.Join("\n - ", warnings)}\n\nPlease tap Close to exit the app, then reopen it to finish the reset.";

                await _dialogService.DisplayAlert("Reset complete", message, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset configuration to factory defaults.");
                await _dialogService.DisplayAlert("Reset failed", $"Unable to reset configuration: {ex.Message}", "OK");
            }
            finally
            {
                _isResetting = false;
                UpdateResetCommandCanExecute();
            }
        }

        private static string ResolveAppDataDirectory(string? overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath;
            }

            try
            {
                return FileSystem.AppDataDirectory;
            }
            catch
            {
                return Path.GetTempPath();
            }
        }

        private bool TryDeleteFile(string path, List<string> warnings, string displayName)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                warnings.Add($"{displayName}: {ex.Message}");
                return false;
            }
        }

        private void UpdateResetCommandCanExecute()
        {
            if (ResetCommand is Command command)
            {
                command.ChangeCanExecute();
            }
        }

        private void NetConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                _dispatcher.Dispatch(() =>
                {
                switch (e.PropertyName)
                {
                    case nameof(NetConnectConfig.BaseFusionAuthURL):
                        OnPropertyChanged(nameof(BaseFusionAuthURL));
                        break;
                    case nameof(NetConnectConfig.ClientId):
                        OnPropertyChanged(nameof(ClientId));
                        break;
                    case nameof(NetConnectConfig.LocalSystemUrl):
                        OnPropertyChanged(nameof(LocalSystemUrlDisplay));
                        break;
                    case nameof(NetConnectConfig.AppID):
                        OnPropertyChanged(nameof(AppID));
                        break;
                    case nameof(NetConnectConfig.FilterStrategies):
                        OnPropertyChanged(nameof(FilterStrategies));
                        break;
                    case nameof(NetConnectConfig.OqsProviderPath):
                        OnPropertyChanged(nameof(OqsProviderPath));
                        break;
                    case nameof(NetConnectConfig.ClientAuthUrl):
                        OnPropertyChanged(nameof(ClientAuthUrl));
                        break;
                    case nameof(NetConnectConfig.AuthKey):
                        OnPropertyChanged(nameof(AuthKey));
                        break;
                    case nameof(NetConnectConfig.Owner):
                        OnPropertyChanged(nameof(Owner));
                        break;
                    case nameof(NetConnectConfig.MonitorLocation):
                        OnPropertyChanged(nameof(MonitorLocation));
                        break;
                    default:
                        // If the property name does not match any known properties, you might choose to log this or handle it as needed.
                        // This could be useful for debugging or if you're expecting other properties to change that are not listed here.
                        break;
                }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling property change in ConfigPageViewModel. Error was: {ex}");
                // Optionally, handle the exception, such as reverting changes or notifying the user.
            }
        }
        public string BaseFusionAuthURL => _netConfig.BaseFusionAuthURL;
        public string ClientId => _netConfig.ClientId;
        public string? LocalSystemUrlDisplay => _netConfig.LocalSystemUrl?.ExternalUrl;
        public string AppID => _netConfig.AppID;
        public List<FilterStrategyConfig> FilterStrategies => _netConfig.FilterStrategies;
        public int MaxTaskQueueSize => _netConfig.MaxTaskQueueSize;
        public string OqsProviderPath => _netConfig.OqsProviderPath;
        public string ClientAuthUrl => _netConfig.ClientAuthUrl;
        public string AuthKey => _netConfig.AuthKey;
        public string Owner => _netConfig.Owner;
        public string MonitorLocation => _netConfig.MonitorLocation;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
