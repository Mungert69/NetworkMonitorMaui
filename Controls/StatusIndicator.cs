using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
namespace NetworkMonitor.Maui.Controls;

public class StatusIndicator : ContentView
{
    private BoxView _ripple;
    private BoxView _circle;
    private Timer _animationTimer;
    CancellationTokenSource _animationCts;
    public static readonly BindableProperty IsUpProperty = BindableProperty.Create(
        nameof(IsUp), typeof(bool), typeof(StatusIndicator), default(bool), propertyChanged: OnIsUpChanged);

    public bool IsUp
    {
        get => (bool)GetValue(IsUpProperty);
        set
        {
            if (IsUp != value)
            {
                SetValue(IsUpProperty, value);
                // Additional logic if needed
            }
        }
    }

    private static void OnIsUpChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StatusIndicator)bindable;
        control.UpdateVisualState();
    }


    public static readonly BindableProperty PacketsLostPercentageProperty = BindableProperty.Create(
        nameof(PacketsLostPercentage), typeof(double), typeof(StatusIndicator), default(double));

    public double PacketsLostPercentage
    {
        get => (double)GetValue(PacketsLostPercentageProperty);
        set
        {
            if (PacketsLostPercentage != value)
            {
                SetValue(PacketsLostPercentageProperty, value);
                // Additional logic if needed
            }
        }

    }

    public static readonly BindableProperty RoundTripTimeAverageProperty = BindableProperty.Create(
       nameof(RoundTripTimeAverage), typeof(double), typeof(StatusIndicator), default(double));

    public double RoundTripTimeAverage
    {
        get => (double)GetValue(RoundTripTimeAverageProperty);
        set
        {
            if (RoundTripTimeAverage != value)
            {
                SetValue(RoundTripTimeAverageProperty, value);
                // Additional logic if needed
            }
        }

    }


    public static readonly BindableProperty DiameterPixelsProperty = BindableProperty.Create(
          nameof(DiameterPixels), typeof(double), typeof(StatusIndicator), default(double), propertyChanged: OnDiameterPixelsChanged);

    public double DiameterPixels
    {
        get => (double)GetValue(DiameterPixelsProperty);
        set
        {
            if (DiameterPixels != value)
            {
                SetValue(DiameterPixelsProperty, value);
                // Additional logic if needed
            }
        }

    }

    private static void OnDiameterPixelsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StatusIndicator)bindable;

        control.UpdateIndicatorSize((double)newValue);

    }



    public static readonly BindableProperty IsAnimatedProperty = BindableProperty.Create(
    nameof(IsAnimated), typeof(bool), typeof(StatusIndicator), true, propertyChanged: OnIsAnimatedChanged);

    public bool IsAnimated
    {
        get => (bool)GetValue(IsAnimatedProperty);

        set
        {
            if (IsAnimated != value)
            {
                SetValue(IsAnimatedProperty, value);
                if (value == true)
                {
                    _animationCts?.Dispose(); // Dispose the old CTS if it exists
                    _animationCts = new CancellationTokenSource();
                    var pulsingTask = StartPulsingAnimation(_animationCts.Token);
                    var rippleTask = StartRippleAnimation(_animationCts.Token);

                    // Start the timer
                    _animationTimer.Start();
                }
                // Additional logic if needed
            }
        }
    }

    private static void OnIsAnimatedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StatusIndicator)bindable;
        control.UpdateAnimationState((bool)newValue);
    }

    public StatusIndicator()
    {
        _circle = new BoxView
        {
            WidthRequest = 30,
            HeightRequest = 30,
            CornerRadius = 15,
            Color = ColorResource.GetResourceColor("Error"),
            //Background = ColorResource.GetResourceColor("White")
        };

        _ripple = new BoxView
        {
            WidthRequest = 30,
            HeightRequest = 30,
            CornerRadius = 15,
            Opacity = 0, // Initially invisible
            Color = ColorResource.GetResourceColor("Secondary"),
            //Background = ColorResource.GetResourceColor("White")
        };

        // Add the ripple to the layout
        var layout = new AbsoluteLayout();
        AbsoluteLayout.SetLayoutBounds(_circle, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(_circle, AbsoluteLayoutFlags.PositionProportional);
        layout.Children.Add(_circle);

        AbsoluteLayout.SetLayoutBounds(_ripple, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(_ripple, AbsoluteLayoutFlags.PositionProportional);
        layout.Children.Add(_ripple);

        Content = layout;
        _animationCts = new CancellationTokenSource();
        _animationTimer = new Timer(50000); // 1 minute duration
        _animationTimer.AutoReset = false; // Ensures the timer runs only once
        _animationTimer.Elapsed += OnAnimationTimerElapsed;
        _animationTimer.Start();
    }



    private void UpdateIndicatorSize(double diameter)
    {
        if (_circle != null)
        {
            _circle.WidthRequest = diameter;
            _circle.HeightRequest = diameter;
            _circle.CornerRadius = (float)(diameter / 2);
        }

        if (_ripple != null)
        {
            _ripple.WidthRequest = diameter;
            _ripple.HeightRequest = diameter;
            _ripple.CornerRadius = (float)(diameter / 2);
        }
    }
    public void UpdateVisualState()
    {

        // Explicitly stop the animation if IsUp is false
        if (!IsUp)
        {
            _circle.Color = ColorResource.GetResourceColor("Error");
            _circle.CancelAnimations(); // This stops any ongoing animations
            _ripple.CancelAnimations();
        }
        else
        {
            _circle.Color = ColorResource.GetResourceColor("Primary");
            UpdateAnimationState(IsAnimated); // Control animations based on IsAnimated

        }

    }

    private void UpdateAnimationState(bool isAnimated)
    {
        if (isAnimated && IsUp)
        {
            _animationCts?.Dispose(); // Dispose the old CTS if it exists
            _animationCts = new CancellationTokenSource();
            var pulsingTask = StartPulsingAnimation(_animationCts.Token);
            var rippleTask = StartRippleAnimation(_animationCts.Token);

            // Start the timer
            _animationTimer.Start();
        }
        else
        {
            _circle.CancelAnimations();
            _ripple.CancelAnimations();
        }
    }
    private void OnAnimationTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        this.Dispatcher.Dispatch(() =>
   {
       _animationCts.Cancel();
   });
    }

    private async Task StartPulsingAnimation(CancellationToken cancellationToken)
    {
        if (!IsUp || !IsAnimated) return;

        while (!cancellationToken.IsCancellationRequested && IsUp && IsAnimated)
        {
            uint animationDuration = CalculateAnimationDuration(RoundTripTimeAverage);
            await _circle.ScaleTo(1.2, animationDuration);
            await _circle.ScaleTo(1.0, animationDuration);
        }



    }

    private async Task StartRippleAnimation(CancellationToken cancellationToken)
    {
        if (!IsUp || !IsAnimated) return;

        _ripple.Opacity = 0.07;
        _ripple.Scale = 1;
        double scale = CalculateRippleScale(PacketsLostPercentage);

        while (!cancellationToken.IsCancellationRequested && IsUp && IsAnimated)
        {
            uint animationDuration = CalculateRippleAnimationDuration(PacketsLostPercentage);
            await _ripple.ScaleTo(scale, animationDuration);
            await _ripple.FadeTo(0, animationDuration);
            await StartRippleAnimation(cancellationToken);
        }


    }

    private double CalculateRippleScale(double packetsLostPercentage)
    {
        if (packetsLostPercentage <= 10)
        {
            // Rapid decrease in scale from 0% to 10%
            // Scale decreases from 8.0 (at 0%) to a lower value (e.g., 2.0) at 10%
            return 8.0 - ((packetsLostPercentage / 10.0) * 6.0); // Decreases rapidly
        }
        else
        {
            // Gradual decrease in scale for percentages above 10%
            // This part of the formula can be adjusted as needed
            // Continues to decrease from 2.0 at 10% down to a minimum scale at 100%
            return 2.0 - ((packetsLostPercentage - 10) / 90.0) * 1.0; // Decreases slowly
        }
    }

    private uint CalculateRippleAnimationDuration(double packetsLostPercentage)
    {
        // Assuming a higher percentage means a slower animation
        // You can adjust the formula as per your requirement

        // Base duration (e.g., 1000 milliseconds for 0% loss)
        double baseDuration = 2000;

        // Adjust duration based on packet loss percentage
        // Example: Increase duration by up to double for 100% packet loss
        double adjustedDuration = baseDuration + (baseDuration * (packetsLostPercentage / 100.0));

        return (uint)adjustedDuration;
    }


    private uint CalculateAnimationDuration(double roundTripTimeAverage)
    {
        // Assuming lower average time means faster pulsing.
        // The duration of the animation increases as the round-trip time increases.

        // You can add a lower limit to ensure the animation is not too fast
        double minDuration = 300; // Minimum animation duration in milliseconds

        // You can also add an upper limit to ensure the animation is not too slow
        double maxDuration = 10000; // Maximum animation duration in milliseconds

        // Directly use roundTripTimeAverage, possibly scaling it if needed
        double duration = roundTripTimeAverage; // Simple direct assignment

        // Ensuring the duration is within the min and max limits
        duration = Math.Max(minDuration, Math.Min(duration, maxDuration));

        return (uint)duration;
    }


    public void Cleanup()
    {
        _animationCts.Cancel();
        _animationCts.Dispose();
        _circle.CancelAnimations();
        _ripple.CancelAnimations();
    }
    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent == null)
        {
            // Control is being removed from the visual tree
            Cleanup();
        }
    }

}
