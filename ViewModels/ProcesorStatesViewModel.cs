using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
namespace NetworkMonitor.Maui.ViewModels
{
    public class ProcessorStatesViewModel : BasePopupViewModel
    {
        private LocalProcessorStates _processorStates;
        public ICommand ShowPopupCommand { get; private set; }
        private ILogger _logger;
        private string _popupMessageType = "";
        // Local backing fields
        private bool _isRunning;
        private bool _isSetup;
        private ConnectState _isConnectState;
        private bool _isRabbitConnected;
        private string _setupMessage;
        private string _rabbitSetupMessage;
        private string _runningMessage;
        private string _connectRunningMessage;

        public ProcessorStatesViewModel(ILogger logger, LocalProcessorStates processorStates)
        {
            try
            {
                // _logger = MauiProgram.ServiceProvider.GetRequiredService<ILogger<ProcessorStatesViewModel>>();
                // _processorStates = MauiProgram.ServiceProvider.GetRequiredService<LocalProcessorStates>();
                _logger = logger; _processorStates = processorStates;

                // Initialize local copies
                _isRunning = _processorStates.IsRunning;
                _isSetup = _processorStates.IsSetup;
                _isConnectState = _processorStates.IsConnectState;
                _isRabbitConnected = _processorStates.IsRabbitConnected;
                _setupMessage = _processorStates.SetupMessage;
                _rabbitSetupMessage = _processorStates.RabbitSetupMessage;
                _runningMessage = _processorStates.RunningMessage;
                _connectRunningMessage = _processorStates.ConnectRunningMessage;

                // Subscribe to the PropertyChanged event of _processorStates if it implements INotifyPropertyChanged
                _processorStates.PropertyChanged += OnProcessorStatesChanged;
                ShowPopupCommand = new Command<string>(ShowPopupWithMessage);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error initializing ProcessorStatesViewModel: {ex}");
                // Consider fallback logic or notifying the user of the error
            }


        }


        // Public properties bound to the UI
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSetup
        {
            get => _isSetup;
            private set
            {
                if (_isSetup != value)
                {
                    _isSetup = value;
                    OnPropertyChanged();
                }
            }
        }

        public ConnectState IsConnectState
        {
            get => _isConnectState;
            private set
            {
                if (_isConnectState != value)
                {
                    _isConnectState = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRabbitConnected
        {
            get => _isRabbitConnected;
            private set
            {
                if (_isRabbitConnected != value)
                {
                    _isRabbitConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SetupMessage
        {
            get => _setupMessage;
            private set
            {
                if (_setupMessage != value)
                {
                    _setupMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RabbitSetupMessage
        {
            get => _rabbitSetupMessage;
            private set
            {
                if (_rabbitSetupMessage != value)
                {
                    _rabbitSetupMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RunningMessage
        {
            get => _runningMessage;
            private set
            {
                if (_runningMessage != value)
                {
                    _runningMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectRunningMessage
        {
            get => _connectRunningMessage;
            private set
            {
                if (_connectRunningMessage != value)
                {
                    _connectRunningMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnProcessorStatesChanged(object? sender, PropertyChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Update the corresponding local property based on the changed property name
                switch (e.PropertyName)
                {
                    case nameof(_processorStates.IsRunning):
                        IsRunning = _processorStates.IsRunning;
                        break;
                    case nameof(_processorStates.IsSetup):
                        IsSetup = _processorStates.IsSetup;
                        break;
                    case nameof(_processorStates.IsConnectState):
                        IsConnectState = _processorStates.IsConnectState;
                        break;
                    case nameof(_processorStates.IsRabbitConnected):
                        IsRabbitConnected = _processorStates.IsRabbitConnected;
                        break;
                    case nameof(_processorStates.SetupMessage):
                        SetupMessage = _processorStates.SetupMessage;
                        break;
                    case nameof(_processorStates.RabbitSetupMessage):
                        RabbitSetupMessage = _processorStates.RabbitSetupMessage;
                        break;
                    case nameof(_processorStates.RunningMessage):
                        RunningMessage = _processorStates.RunningMessage;
                        break;
                    case nameof(_processorStates.ConnectRunningMessage):
                        ConnectRunningMessage = _processorStates.ConnectRunningMessage;
                        break;
                }

                if (IsPopupVisible)
                {
                    UpdatePopupMessage(e.PropertyName);
                }
            });
        }

       
        private void UpdatePopupMessage(string? propertyName)
        {
            // Logic to update PopupMessage based on propertyName
            switch (propertyName)
            {
                case nameof(RunningMessage):
                    if (_popupMessageType == "RunningMessage") PopupMessage = $"Running Message: {RunningMessage}";
                    break;
                case nameof(ConnectRunningMessage):
                    if (_popupMessageType == "ConnectRunningMessage") PopupMessage = $"Monitor Message: {ConnectRunningMessage}";

                    break;
                case nameof(SetupMessage):
                    if (_popupMessageType == "SetupMessage") PopupMessage = $"Running Message: {RunningMessage}";
                    PopupMessage = $"Setup Message: {SetupMessage}";
                    break;
                case nameof(RabbitSetupMessage):
                    if (_popupMessageType == "RabbitSetupMessage") PopupMessage = $"Rabbit Setup Message: {RabbitSetupMessage}";
                    break;
                    // Add additional cases as needed
            }
        }


        private void ShowPopupWithMessage(string messageType)
        {
            _popupMessageType = messageType;
            switch (messageType)
            {
                case "RunningMessage":
                    PopupMessage = $"Running Message: {RunningMessage}";
                    break;
                case "ConnectRunningMessage":
                    PopupMessage = $"Monitor Message: {ConnectRunningMessage}";

                    break;
                case "SetupMessage":
                    PopupMessage = $"Setup Message: {SetupMessage}";
                    break;
                case "RabbitSetupMessage":
                    PopupMessage = $"Rabbit Setup Message: {RabbitSetupMessage}";
                    break;
                    // Add additional cases as needed
            }
            IsPopupVisible = true;
        }



    }
}
