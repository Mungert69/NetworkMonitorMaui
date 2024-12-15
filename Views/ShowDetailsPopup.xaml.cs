using CommunityToolkit.Maui.Views;

namespace NetworkMonitorAgent
{
    public partial class StatusDetailsPopup : Popup
    {
        public StatusDetailsPopup()
        {
            InitializeComponent(); var rootWrapper = new Grid
            {
                BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#1E1E1E") // Dark theme
            : Colors.White,             // Light theme
                Padding = new Thickness(0),    // Add padding around the popup content
                Margin = new Thickness(0)
            };

            // Wrap the popup content
            if (Content != null)
            {
                rootWrapper.Children.Add(Content);
                Content = rootWrapper;
            }

        }
        public async void OnDetailsButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await CloseAsync(true, cts.Token);
            }
            catch { }

        }

        public async void OnCloseButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await CloseAsync(false, cts.Token);
            }
            catch { }

        }

        // Additional methods or event handlers
    }
}
