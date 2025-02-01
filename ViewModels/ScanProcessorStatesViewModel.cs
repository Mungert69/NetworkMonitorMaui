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
namespace NetworkMonitor.Maui.ViewModels
{
    public class ScanProcessorStatesViewModel : BasePopupViewModel
    {
        private readonly ILocalCmdProcessorStates _cmdProcessorStates;
        private readonly ILogger _logger;
        private readonly IApiService _apiService;
        public ObservableCollection<string> EndpointTypes { get; set; }
        public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces =>
           new ObservableCollection<NetworkInterfaceInfo>(_cmdProcessorStates.AvailableNetworkInterfaces);
        public string RunningMessage => _cmdProcessorStates.RunningMessage;
        public string CompletedMessage => _cmdProcessorStates.CompletedMessage;
        public NetworkInterfaceInfo SelectedNetworkInterface
        {
            get => _cmdProcessorStates.SelectedNetworkInterface;
            set
            {
                _cmdProcessorStates.SelectedNetworkInterface = value;
                OnPropertyChanged();
            }
        }


        public ScanProcessorStatesViewModel(ILogger<ScanProcessorStatesViewModel> logger, ILocalCmdProcessorStates cmdProcessorStates, IApiService apiService,NetConnectConfig netConfig)
        {
            try
            {
                _logger = logger;
                _cmdProcessorStates = cmdProcessorStates;
                _cmdProcessorStates.EndpointTypes = netConfig.EndpointTypes;
                _cmdProcessorStates.UseDefaultEndpointType = netConfig.UseDefaultEndpointType;
                _cmdProcessorStates.DefaultEndpointType = netConfig.DefaultEndpointType;
                _apiService = apiService;
                _cmdProcessorStates.PropertyChanged += OnProcessorStatesChanged;
                EndpointTypes = new ObservableCollection<string>(_cmdProcessorStates.EndpointTypes);
                LoadNetworkInterfaces();

            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error initializing ScanProcessorStatesViewModel: {ex}");
            }


        }

        public List<MonitorIP> SelectedDevices => _cmdProcessorStates.SelectedDevices.ToList();
        public void LoadNetworkInterfaces()
        {
            if (_cmdProcessorStates != null)
            {
                _cmdProcessorStates.AvailableNetworkInterfaces = NetworkUtils.GetSuitableNetworkInterfaces(_logger, _cmdProcessorStates);
                if (_cmdProcessorStates.AvailableNetworkInterfaces != null && _cmdProcessorStates.AvailableNetworkInterfaces.Count > 0) _cmdProcessorStates.SelectedNetworkInterface = _cmdProcessorStates.AvailableNetworkInterfaces.First();

            }

        }
        public async Task Scan()
        {
            IsPopupVisible = true;
            await _cmdProcessorStates.Scan();
        }
        public async Task Cancel()
        {
            await _cmdProcessorStates.Cancel();
        }

        public string DefaultEndpointType
        {
            get => _cmdProcessorStates.DefaultEndpointType;
            set
            {
                _cmdProcessorStates.DefaultEndpointType = value;
                OnPropertyChanged();
            }
        }

        public bool UseDefaultEndpointType
        {
            get => _cmdProcessorStates.UseDefaultEndpointType;
            set
            {
                _cmdProcessorStates.UseDefaultEndpointType = value;
                OnPropertyChanged();
            }
        }
        public bool UseFastScan
        {
            get => _cmdProcessorStates.UseFastScan;
            set
            {
                _cmdProcessorStates.UseFastScan = value;
                OnPropertyChanged();
            }
        }
        public bool LimitPorts
        {
            get => _cmdProcessorStates.LimitPorts;
            set
            {
                _cmdProcessorStates.LimitPorts = value;
                OnPropertyChanged();
            }
        }

        private void OnProcessorStatesChanged(object? sender, PropertyChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged(e.PropertyName);

                        if (IsPopupVisible)
                        {
                            UpdatePopupMessage(e.PropertyName);
                        }
                    });
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
            // Reset any previous state
            IsPopupVisible = true;
            await _cmdProcessorStates.Scan();

            // Return the detected hosts
            return _cmdProcessorStates.ActiveDevices.ToList();
        }

        public async Task AddServices()
        {
            await _cmdProcessorStates.AddServices();
        }

        public void AddSelectedHosts(List<MonitorIP> selectedServices)
        {
            _cmdProcessorStates.SelectedDevices.Clear();
            foreach (var service in selectedServices)
            {
                _cmdProcessorStates.SelectedDevices.Add(service);
            }
            //IsPopupVisible = false;
        }
        public async Task CheckServices()
        {
            // Convert the selected devices to a list of IConnectionObject (HostObject)
            var connectionObjects = new List<IConnectionObject>();

            foreach (var device in _cmdProcessorStates.SelectedDevices)
            {
                IConnectionObject hostObject;
                if (device.EndPointType == "quantum")
                {
                    hostObject = new QuantumHostObject
                    {
                        Address = device.Address ?? "NoHostFound", // Assuming MonitorIP has a property IPAddress
                        Port = device.Port,         // Assuming MonitorIP has a property Port
                        Timeout = 10000             // Default timeout, can be customized
                    };
                }
                else
                {
                    hostObject = new HostObject
                    {
                        Address = device.Address ?? "NoHostFound", // Assuming MonitorIP has a property IPAddress
                        Port = device.Port,         // Assuming MonitorIP has a property Port
                        Timeout = 59000,             // Default timeout, can be customized
                        EndPointType = device.EndPointType ?? "icmp"
                    };
                }


                connectionObjects.Add(hostObject);
            }

            // Use the ApiService to check the connections
            var results = await _apiService.CheckConnections(connectionObjects);
            _cmdProcessorStates.CompletedMessage += "\n\nChecking status of selected services...\n\n";
            // Handle the results (e.g., display them or log them)
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

            // Update the UI or state based on results, if necessary
        }
    }

}