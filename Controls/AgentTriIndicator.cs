using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.Services;

namespace NetworkMonitor.Maui.Controls
{
    public class AgentTriIndicator : ContentView
    {
        private BoxView circle;
        private IColorResource  ColorResource = ServiceInitializer.RootProvider.ColorResource;
        private CancellationTokenSource _animationCts = new();
        private readonly IUiDispatcher _dispatcher = ServiceInitializer.Dispatcher;
        private bool _isLoaded = false;
        private bool _pendingVisualUpdate = false;

        public static readonly BindableProperty ConnectStateProperty = BindableProperty.Create(nameof(ConnectState), typeof(ConnectState), typeof(AgentTriIndicator), ConnectState.Error, propertyChanged: OnConnectStateChanged);



        public ConnectState ConnectState
        {
            get => (ConnectState)GetValue(ConnectStateProperty);
            set => SetValue(ConnectStateProperty, value);
        }

        private static void OnConnectStateChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (AgentTriIndicator)bindable;
            control.UpdateVisualState();
        }
    public AgentTriIndicator()
        {
           
            circle = new BoxView
            {
                WidthRequest = 25,
                HeightRequest = 25,
                CornerRadius = 12,
                Color = ColorResource.GetResourceColor("Error"),
                Background=new Microsoft.Maui.Graphics.Color(0, 0, 0, 0)
            };

            var layout = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(circle, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(circle, AbsoluteLayoutFlags.PositionProportional);
            layout.Children.Add(circle);

            Content = layout;

            Loaded += (_, __) =>
            {
                _isLoaded = true;
                if (_pendingVisualUpdate)
                {
                    _pendingVisualUpdate = false;
                    UpdateVisualState();
                }
            };
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler != null && !_isLoaded)
            {
                _isLoaded = true;
                UpdateVisualState();
            }
        }

        public void UpdateVisualState()
        {
            if (_dispatcher.IsDispatchRequired)
            {
                _dispatcher.Dispatch(UpdateVisualState);
                return;
            }

            StopAnimations();
            switch (ConnectState)
            {
                case ConnectState.Running:
                    circle.Color = ColorResource.GetResourceColor("Primary");
                    if (!_isLoaded)
                    {
                        _pendingVisualUpdate = true;
                        return;
                    }
                    StartRunningAnimation();
                    break;
                case ConnectState.Waiting:
                    circle.Color = ColorResource.GetResourceColor("Warning");
                    if (!_isLoaded)
                    {
                        _pendingVisualUpdate = true;
                        return;
                    }
                    StartWaitingAnimation();
                    break;
                case ConnectState.Error:
                    circle.Color = ColorResource.GetResourceColor("Error");
                    break;
            }
        }

        private async void StartRunningAnimation()
        {
            if (_dispatcher.IsDispatchRequired)
            {
                _dispatcher.Dispatch(StartRunningAnimation);
                return;
            }
            var token = _animationCts.Token;
            try
            {
                while (!token.IsCancellationRequested && ConnectState == ConnectState.Running)
                {
                    await circle.ScaleToAsync(1.0, 500);
                    await circle.ScaleToAsync(0.9, 500);
                    await Task.Delay(16, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void StartWaitingAnimation()
        {
            if (_dispatcher.IsDispatchRequired)
            {
                _dispatcher.Dispatch(StartWaitingAnimation);
                return;
            }
            var token = _animationCts.Token;
            try
            {
                while (!token.IsCancellationRequested && ConnectState == ConnectState.Waiting)
                {
                    await circle.ScaleToAsync(1.0, 2000);
                    await circle.ScaleTo(0.8, 2000);
                    await Task.Delay(16, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void StopAnimations()
        {
            try { _animationCts.Cancel(); } catch { }
            circle.CancelAnimations();
            _animationCts.Dispose();
            _animationCts = new CancellationTokenSource();
        }
    }
}
