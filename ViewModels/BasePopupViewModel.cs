using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
namespace NetworkMonitor.Maui.ViewModels
{
    public class BasePopupViewModel: INotifyPropertyChanged
    {
        public ICommand ClosePopupCommand { get; protected set; }
        private bool _isPopupVisible;
        private string _popupMessage="";

        public BasePopupViewModel()
        {
            ClosePopupCommand = new Command(() => IsPopupVisible = false);
        }

         public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set
            {
                _isPopupVisible = value;
                OnPropertyChanged();
            }
        }
         public string PopupMessage
        {
            get => _popupMessage;
            set
            {
                _popupMessage = value;
                OnPropertyChanged();
            }
        }

        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
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
