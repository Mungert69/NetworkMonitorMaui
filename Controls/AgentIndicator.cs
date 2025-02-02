using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.Services;
namespace NetworkMonitor.Maui.Controls;
public class AgentIndicator : ContentView
{

    private BoxView circle;
    private IColorResource  ColorResource = ServiceInitializer.RootProvider.ColorResource;

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
        };



        // Add the ripple to the layout
        var layout = new AbsoluteLayout();
        AbsoluteLayout.SetLayoutBounds(circle, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(circle, AbsoluteLayoutFlags.PositionProportional);
        layout.Children.Add(circle);

        Content = layout;
    }

    public void UpdateVisualState()
    {

        if (IsUp)
        {
            circle.Color = ColorResource.GetResourceColor("Primary");
            StartPulsingAnimation();
        }
        else
        {
            circle.Color = ColorResource.GetResourceColor("Error");
            circle.CancelAnimations();
        }
    }


    public async void StartPulsingAnimation()
    {
        // Color originalColor = circle.Color;
        //Color lighterColor = ColorResource.LightenColor(originalColor, 0.1f);
        circle.CancelAnimations();
        while (IsUp) // Continue animation while the indicator is up
        {
            // Scale up and lighten the color
            // ColorResource.AnimateColor(circle, originalColor, lighterColor, 500);
            await circle.ScaleTo(1.0, 500);


            // Scale down and return to the original color
            // ColorResource.AnimateColor(circle, lighterColor, originalColor, 500);
            await circle.ScaleTo(0.9, 500);

        }
    }

    

}
