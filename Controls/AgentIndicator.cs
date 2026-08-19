using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.Services;
namespace NetworkMonitor.Maui.Controls;

public class AgentIndicator : ContentView
{

    private BoxView circle;
    private IColorResource ColorResource = ServiceInitializer.RootProvider.ColorResource;
    private CancellationTokenSource _animationCts = new();
    private readonly IUiDispatcher _dispatcher = ServiceInitializer.Dispatcher;
    private bool _isLoaded = false;
    private bool _pendingPulse = false;

    public static readonly BindableProperty IsUpProperty = BindableProperty.Create(
        nameof(IsUp), typeof(bool), typeof(AgentIndicator), default(bool), propertyChanged: OnIsUpChanged);

    public bool IsUp
    {
        get => (bool)GetValue(IsUpProperty);
        set => SetValue(IsUpProperty, value);
    }


    private static void OnIsUpChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AgentIndicator)bindable;
        control.UpdateVisualState();
    }

    public AgentIndicator()
    {

        circle = new BoxView
        {
            WidthRequest = 25,
            HeightRequest = 25,
            CornerRadius = 12,
            Color = ColorResource.GetResourceColor("Error"),
            Background = new Microsoft.Maui.Graphics.Color(0, 0, 0, 0)
        };



        // Add the ripple to the layout
        var layout = new AbsoluteLayout();
        AbsoluteLayout.SetLayoutBounds(circle, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(circle, AbsoluteLayoutFlags.PositionProportional);
        layout.Children.Add(circle);

        Content = layout;

        Loaded += (_, __) =>
        {
            _isLoaded = true;
            if (IsUp || _pendingPulse)
            {
                _pendingPulse = false;
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
            if (IsUp)
            {
                UpdateVisualState();
            }
        }
    }

    public void UpdateVisualState()
    {
        if (_dispatcher.IsDispatchRequired)
        {
            _dispatcher.Dispatch(UpdateVisualState);
            return;
        }

        if (IsUp)
        {
            circle.Color = ColorResource.GetResourceColor("Primary");
            if (!_isLoaded)
            {
                _pendingPulse = true;
                return;
            }
            StartPulsingAnimation();
        }
        else
        {
            circle.Color = ColorResource.GetResourceColor("Error");
            StopPulsingAnimation();
        }
    }


    public async void StartPulsingAnimation()
    {
        if (_dispatcher.IsDispatchRequired)
        {
            _dispatcher.Dispatch(StartPulsingAnimation);
            return;
        }

        StopPulsingAnimation();
        var token = _animationCts.Token;
        circle.CancelAnimations();

        try
        {
            while (!token.IsCancellationRequested && IsUp)
            {
                await circle.ScaleToAsync(1.0, 500);
                await circle.ScaleToAsync(0.9, 500);

                // Guard against tight loops if animations complete synchronously.
                await Task.Delay(16, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during state changes; swallow to avoid crashing from async-void.
        }
        catch (Exception ex)
        {
            // Never throw out of async-void on Android; swallow/log best-effort.
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void StopPulsingAnimation()
    {
        try { _animationCts.Cancel(); } catch { }
        circle.CancelAnimations();
        _animationCts.Dispose();
        _animationCts = new CancellationTokenSource();
    }



}
