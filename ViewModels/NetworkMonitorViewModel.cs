using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.Factory;

namespace NetworkMonitor.Maui.ViewModels
{
    public class NetworkMonitorViewModel : INotifyPropertyChanged
    {
        private readonly IApiService _apiService;
        private string _selectedEndpointType;
        private string _address;
        private ushort _port;
        private bool _isBusy;
        private bool _hasResult;
        private string _resultMessage;
        private string _responseTime;
        private string _resultStatus;
        private string _internalEndpointType; // New field to store internal type


        public ObservableCollection<string> EndpointTypes { get; }

        public string SelectedEndpointType
        {
            get => _selectedEndpointType;
            set
            {
                _selectedEndpointType = value;
                // Update internal type whenever the friendly name changes
                _internalEndpointType = EndPointTypeFactory.GetInternalType(_selectedEndpointType);
                OnPropertyChanged();
            }
        }

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public ushort Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool HasResult
        {
            get => _hasResult;
            set { _hasResult = value; OnPropertyChanged(); }
        }

        public string ResultMessage
        {
            get => _resultMessage;
            set { _resultMessage = value; OnPropertyChanged(); }
        }

        public string ResponseTime
        {
            get => _responseTime;
            set { _responseTime = value; OnPropertyChanged(); }
        }

        public string ResultStatus
        {
            get => _resultStatus;
            set { _resultStatus = value; OnPropertyChanged(); }
        }

        public ICommand TestConnectionCommand { get; }

        public NetworkMonitorViewModel(IApiService apiService)
        {
            _apiService = apiService;
            // Initialize the EndpointTypes collection with friendly names
            EndpointTypes = new ObservableCollection<string>(EndPointTypeFactory.GetFriendlyNames());

            TestConnectionCommand = new Command(async () => await TestConnection());
        }

        private async Task TestConnection()
        {
            if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(SelectedEndpointType))
            {
                ResultMessage = "Please enter an address and select an endpoint type.";
                HasResult = true;
                return;
            }

            IsBusy = true;
            HasResult = false;

            try
            {
                TResultObj<DataObj> result;
                var hostObject = new HostObject { Address = Address, Port = Port, EndPointType = _internalEndpointType, Timeout=59000 }; // Use internal type
                result = await EndPointTypeFactory.TestConnection(_internalEndpointType, _apiService, hostObject, Address, Port);

               
                ResultMessage = result.Success ? "Connection successful" : "Connection failed";
                if (result.Data != null)
                {
                    if (result.Data.ResponseTime != null) ResponseTime = result?.Data?.ResponseTime?.ToString() ?? UInt16.MaxValue.ToString();
                    if (result.Data.ResultStatus!= null) ResultStatus = result?.Data?.ResultStatus ?? "";
                }

                HasResult = true;
            }
            catch (Exception ex)
            {
                ResultMessage = $"An error occurred: {ex.Message}";
                HasResult = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}