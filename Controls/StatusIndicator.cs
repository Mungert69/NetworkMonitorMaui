using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Maui.Services;

namespace NetworkMonitor.Maui.Controls;

public class StatusIndicator : ContentView
{
    private readonly BoxView _circle;
    private readonly BoxView _ripple;
    private CancellationTokenSource _animationCts;
    private IColorResource ColorResource = ServiceInitializer.RootProvider.ColorResource;

    public static readonly BindableProperty IsUpProperty = BindableProperty.Create(
        nameof(IsUp), typeof(bool), typeof(StatusIndicator), default(bool), propertyChanged: OnIsUpChanged);

    public bool IsUp
    {
        get => (bool)GetValue(IsUpProperty);
        set => SetValue(IsUpProperty, value);
    }

    public static readonly BindableProperty DiameterPixelsProperty = BindableProperty.Create(
        nameof(DiameterPixels), typeof(double), typeof(StatusIndicator), 30.0, propertyChanged: OnDiameterChanged);

    public double DiameterPixels
    {
        get => (double)GetValue(DiameterPixelsProperty);
        set => SetValue(DiameterPixelsProperty, value);
    }

    public static readonly BindableProperty PacketsLostPercentageProperty = BindableProperty.Create(
        nameof(PacketsLostPercentage), typeof(double), typeof(StatusIndicator), 0.0);

    public double PacketsLostPercentage
    {
        get => (double)GetValue(PacketsLostPercentageProperty);
        set => SetValue(PacketsLostPercentageProperty, value);
    }

    public static readonly BindableProperty RoundTripTimeAverageProperty = BindableProperty.Create(
        nameof(RoundTripTimeAverage), typeof(double), typeof(StatusIndicator), 500.0);

    public double RoundTripTimeAverage
    {
        get => (double)GetValue(RoundTripTimeAverageProperty);
        set => SetValue(RoundTripTimeAverageProperty, value);
    }

    public static readonly BindableProperty IsAnimatedProperty = BindableProperty.Create(
        nameof(IsAnimated), typeof(bool), typeof(StatusIndicator), true, propertyChanged: OnIsAnimatedChanged);

    public bool IsAnimated
    {
        get => (bool)GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }

    public StatusIndicator()
    {
        _circle = new BoxView
        {
            Color = ColorResource.GetResourceColor("Error"),
            CornerRadius = 15,
        };

        _ripple = new BoxView
        {
            Color = ColorResource.GetResourceColor("Secondary"),
            CornerRadius = 15,
            Opacity = 0,
        };

        // Wrap in a Grid to give a proper hit area for taps
        var grid = new Grid
        {
            WidthRequest = DiameterPixels,
            HeightRequest = DiameterPixels
        };
        grid.Children.Add(_ripple);
        grid.Children.Add(_circle);

        Content = grid;

        _animationCts = new CancellationTokenSource();

        // Gesture
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => this.SendTapped();
        GestureRecognizers.Add(tapGesture);
    }

    private void SendTapped()
    {
        if (Tapped != null)
            Tapped(this, EventArgs.Empty);
    }

    public event EventHandler Tapped;

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateVisualState();
    }

    private static void OnIsUpChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StatusIndicator)bindable;
        control.UpdateVisualState();
    }

    private static void OnIsAnimatedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StatusIndicator)bindable;
        control.UpdateVisualState();
    }

    private static void OnDiameterChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StatusIndicator)bindable;
        double diameter = (double)newValue;
        control._circle.WidthRequest = diameter;
        control._circle.HeightRequest = diameter;
        control._circle.CornerRadius = (float)(diameter / 2);

        control._ripple.WidthRequest = diameter;
        control._ripple.HeightRequest = diameter;
        control._ripple.CornerRadius = (float)(diameter / 2);

        if (control.Content is Grid grid)
        {
            grid.WidthRequest = diameter;
            grid.HeightRequest = diameter;
        }
    }

    private void UpdateVisualState()
    {
        _circle.Color = IsUp ? ColorResource.GetResourceColor("Primary") : ColorResource.GetResourceColor("Error");

        if (!IsUp || !IsAnimated)
        {
            StopAnimations();
        }
        else
        {
            StartAnimations();
        }
    }

    private void StopAnimations()
    {
        _animationCts.Cancel();
        _circle.CancelAnimations();
        _ripple.CancelAnimations();
        _animationCts = new CancellationTokenSource();
    }

    private void StartAnimations()
    {
        StopAnimations();
        var token = _animationCts.Token;
        _ = RunPulseAnimation(token);
        _ = RunRippleAnimation(token);
    }

    private async Task RunPulseAnimation(CancellationToken token)
    {
        while (!token.IsCancellationRequested && IsUp && IsAnimated)
        {
            uint duration = CalculateAnimationDuration(RoundTripTimeAverage);
            await _circle.ScaleToAsync(1.2, duration);
            await _circle.ScaleToAsync(1.0, duration);
        }
    }

    private async Task RunRippleAnimation(CancellationToken token)
    {
        _ripple.Opacity = 0.07;
        _ripple.Scale = 1;
        double scale = CalculateRippleScale(PacketsLostPercentage);

        while (!token.IsCancellationRequested && IsUp && IsAnimated)
        {
            uint duration = CalculateRippleAnimationDuration(PacketsLostPercentage);
            await _ripple.ScaleToAsync(scale, duration);
            await _ripple.FadeToAsync(0, duration);
            _ripple.Scale = 1;
            _ripple.Opacity = 0.07;
        }
    }

    private double CalculateRippleScale(double packetsLostPercentage)
    {
        return packetsLostPercentage <= 10
            ? 8.0 - ((packetsLostPercentage / 10.0) * 6.0)
            : 2.0 - ((packetsLostPercentage - 10) / 90.0);
    }

    private uint CalculateRippleAnimationDuration(double packetsLostPercentage)
    {
        double baseDuration = 2000;
        double adjusted = baseDuration + (baseDuration * (packetsLostPercentage / 100.0));
        return (uint)adjusted;
    }

    private uint CalculateAnimationDuration(double roundTripTime)
    {
        double min = 300;
        double max = 10000;
        double duration = roundTripTime;
        duration = Math.Max(min, Math.Min(duration, max));
        return (uint)duration;
    }

    public void Cleanup()
    {
        StopAnimations();
    }
}
