using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Connection;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Logging;

namespace NetworkMonitor.Maui.ViewModels
{
    public class ConfigPageViewModel : INotifyPropertyChanged
    {
        private readonly NetConnectConfig _netConfig;
        private readonly ILogger _logger;

        public ConfigPageViewModel(ILogger<ConfigPageViewModel> logger, NetConnectConfig netConfig)
        {
            try {
                _logger = logger;
                _netConfig = netConfig;
                _netConfig.PropertyChanged += NetConfig_PropertyChanged;
                  }
            catch (Exception ex)
            {
                _logger?.LogError($" Error : initializing ConfigPageViewModel. Error was: {ex}");
            }

        }
        private void NetConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                 MainThread.BeginInvokeOnMainThread(() =>
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
                }});
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
