using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using NetworkMonitor.Connection;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using NetworkMonitor.Utils;
using NetworkMonitor.Api.Services;
using System.Linq;
using System.Threading;
using NetworkMonitor.Maui.Services;

namespace NetworkMonitor.Maui.ViewModels
{
    public class ScanProcessorStatesViewModel : BasePopupViewModel
    {
        // Make states nullable until Initialize is called
        private ILocalCmdProcessorStates? _cmdProcessorStates;
        private readonly ILogger _logger;
        private readonly IApiService _apiService;
        private readonly ICmdProcessorProvider _cmdProcessorProvider;
        private readonly NetConnectConfig _netConfig;
        private readonly IUiDispatcher _dispatcher;

        // initialization guard
        private int _initialized = 0;
        private readonly object _initLock = new();

        public ObservableCollection<string> EndpointTypes { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces =>
           new ObservableCollection<NetworkInterfaceInfo>(_cmdProcessorStates?.AvailableNetworkInterfaces ?? Enumerable.Empty<NetworkInterfaceInfo>());

        public string RunningMessage => _cmdProcessorStates?.RunningMessage ?? string.Empty;
        public string CompletedMessage => _cmdProcessorStates?.CompletedMessage ?? string.Empty;

        public NetworkInterfaceInfo? SelectedNetworkInterface
        {
            get => _cmdProcessorStates?.SelectedNetworkInterface;
            set
            {
                if (_cmdProcessorStates == null) return;
                _cmdProcessorStates.SelectedNetworkInterface = value!;
                OnPropertyChanged();
            }
        }

        // Constructor now lightweight: store dependencies but do NOT call provider methods here.
        // Follows ExitPageViewModel pattern: optional dispatcher parameter, fallback to ServiceInitializer.Dispatcher
        public ScanProcessorStatesViewModel(
            ILogger<ScanProcessorStatesViewModel> logger,
            ICmdProcessorProvider cmdProcessorProvider,
            IApiService apiService,
            NetConnectConfig netConfig,
            IUiDispatcher? dispatcher = null)
        {
            try
            {
                _logger = logger;
                _cmdProcessorProvider = cmdProcessorProvider;
                _apiService = apiService;
                _netConfig = netConfig;
                _dispatcher = dispatcher ?? ServiceInitializer.Dispatcher;
                // keep EndpointTypes non-null for bindings
                EndpointTypes = new ObservableCollection<string>();
                // Do not fetch processor states here — call Initialize later when the app is ready.
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error initializing ScanProcessorStatesViewModel (ctor): {ex}");
            }
        }

        // Public, idempotent initializer. Safe to call multiple times.
        public void Initialize()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 1)
            {
                // already initialized
                return;
            }

            lock (_initLock)
            {
                try
                {
                    _cmdProcessorStates = _cmdProcessorProvider.GetProcessorStates("Nmap");
                    if (_cmdProcessorStates == null)
                    {
                        _logger?.LogWarning("CmdProcessorProvider returned null for 'Nmap'. Initialization deferred or not available.");
                        return;
                    }

                    _cmdProcessorStates.EndpointTypes = _netConfig.EndpointTypes ?? new List<string>();
                    _cmdProcessorStates.UseDefaultEndpointType = _netConfig.UseDefaultEndpointType;
                    _cmdProcessorStates.DefaultEndpointType = _netConfig.DefaultEndpointType;
                    _cmdProcessorStates.PropertyChanged += OnProcessorStatesChanged;

                    EndpointTypes = new ObservableCollection<string>(_cmdProcessorStates.EndpointTypes ?? Enumerable.Empty<string>());
                    OnPropertyChanged(nameof(EndpointTypes));

                    LoadNetworkInterfaces();
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error initializing ScanProcessorStatesViewModel (Initialize): {ex}");
                }
            }
        }

        public List<MonitorIP> SelectedDevices => _cmdProcessorStates?.SelectedDevices?.ToList() ?? new List<MonitorIP>();

        public void LoadNetworkInterfaces()
        {
            if (_cmdProcessorStates != null)
            {
                _cmdProcessorStates.AvailableNetworkInterfaces = NetworkUtils.GetSuitableNetworkInterfaces(_logger, _cmdProcessorStates);
                if (_cmdProcessorStates.AvailableNetworkInterfaces != null && _cmdProcessorStates.AvailableNetworkInterfaces.Count > 0)
                    _cmdProcessorStates.SelectedNetworkInterface = _cmdProcessorStates.AvailableNetworkInterfaces.First();
            }
        }

        public async Task Scan()
        {
            if (_cmdProcessorStates == null) return;
            IsPopupVisible = true;
            await _cmdProcessorStates.Scan();
        }
        public async Task Cancel()
        {
            if (_cmdProcessorStates == null) return;
            await _cmdProcessorStates.Cancel();
        }

        public string DefaultEndpointType
        {
            get => _cmdProcessorStates?.DefaultEndpointType ?? string.Empty;
            set
            {
                if (_cmdProcessorStates == null) return;
                _cmdProcessorStates.DefaultEndpointType = value;
                OnPropertyChanged();
            }
        }

        public bool UseDefaultEndpointType
        {
            get => _cmdProcessorStates?.UseDefaultEndpointType ?? false;
            set
            {
                if (_cmdProcessorStates == null) return;
                _cmdProcessorStates.UseDefaultEndpointType = value;
                OnPropertyChanged();
            }
        }
        public bool UseFastScan
        {
            get => _cmdProcessorStates?.UseFastScan ?? false;
            set
            {
                if (_cmdProcessorStates == null) return;
                _cmdProcessorStates.UseFastScan = value;
                OnPropertyChanged();
            }
        }
        public bool LimitPorts
        {
            get => _cmdProcessorStates?.LimitPorts ?? false;
            set
            {
                if (_cmdProcessorStates == null) return;
                _cmdProcessorStates.LimitPorts = value;
                OnPropertyChanged();
            }
        }

        private void OnProcessorStatesChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // Use dispatcher pattern from ExitPageViewModel to marshal updates to UI thread
                _dispatcher.Dispatch(() =>
                {
                    OnPropertyChanged(e.PropertyName);

                    if (IsPopupVisible)
                    {
                        UpdatePopupMessage(e.PropertyName);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching processor state change");
            }
        }

        private void UpdatePopupMessage(string? propertyName)
        {
            switch (propertyName)
            {
                case nameof(RunningMessage):
                    PopupMessage = $"{RunningMessage}\n{CompletedMessage}";
                    break;
                case nameof(CompletedMessage):
                    PopupMessage = $"{RunningMessage}\n{CompletedMessage}";
                    break;
            }
        }

        public async Task<List<MonitorIP>> ScanForHosts()
        {
            if (_cmdProcessorStates == null) return new List<MonitorIP>();
            IsPopupVisible = true;
            await _cmdProcessorStates.Scan();
            return _cmdProcessorStates.ActiveDevices.ToList();
        }

        public async Task AddServices()
        {
            if (_cmdProcessorStates == null) return;
            await _cmdProcessorStates.AddServices();
        }

        public void AddSelectedHosts(List<MonitorIP> selectedServices)
        {
            if (_cmdProcessorStates == null) return;
            _cmdProcessorStates.SelectedDevices.Clear();
            foreach (var service in selectedServices)
            {
                _cmdProcessorStates.SelectedDevices.Add(service);
            }
        }

        public async Task CheckServices()
        {
            if (_cmdProcessorStates == null) return;
            if (_cmdProcessorStates.SelectedDevices == null || _cmdProcessorStates.SelectedDevices.Count == 0)
            {
                PopupMessage = "Select at least one host to check.";
                IsPopupVisible = true;
                return;
            }

            var connectionObjects = new List<IConnectionObject>();

            foreach (var device in _cmdProcessorStates.SelectedDevices)
            {
                IConnectionObject hostObject;
                if (device.EndPointType == "quantum")
                {
                    hostObject = new QuantumHostObject
                    {
                        Address = device.Address ?? "NoHostFound",
                        Port = device.Port,
                        Timeout = 10000
                    };
                }
                else
                {
                    hostObject = new HostObject
                    {
                        Address = device.Address ?? "NoHostFound",
                        Port = device.Port,
                        Timeout = 59000,
                        EndPointType = device.EndPointType ?? "icmp"
                    };
                }

                connectionObjects.Add(hostObject);
            }

            var results = await _apiService.CheckConnections(connectionObjects);
            _cmdProcessorStates.CompletedMessage += "\n\nChecking status of selected services...\n\n";

            foreach (var result in results)
            {
                string message = "No Data in results";
                if (result.Data != null)
                {
                    if (result.Success)
                    {
                        message = $"Performed a successful {result.Data.CheckPerformed} check for {result.Data.TestedAddress} on port {result.Data.TestedPort} with status {result.Data.ResultStatus}\n";
                        _logger.LogInformation(message);
                    }
                    else
                    {
                        message = $"{result.Data.CheckPerformed} check failed for {result.Data.TestedAddress} on port {result.Data.TestedPort} with status {result.Data.ResultStatus}\n";
                        _logger.LogWarning(message);
                    }
                }
                _cmdProcessorStates.CompletedMessage += message;
            }
        }
    }

}