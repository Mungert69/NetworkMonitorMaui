using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using NetworkMonitor.Objects; 
using NetworkMonitor.Maui;

namespace NetworkMonitor.Maui.Controls
{
    public class AgentTriIndicator : ContentView
    {
        private BoxView circle;

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
                Color = ColorResource.GetResourceColor("Error"), // Default color
            };

            var layout = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(circle, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(circle, AbsoluteLayoutFlags.PositionProportional);
            layout.Children.Add(circle);

            Content = layout;
        }

        public void UpdateVisualState()
        {
            circle.CancelAnimations();
            switch (ConnectState)
            {
                case ConnectState.Running:
                    circle.Color = ColorResource.GetResourceColor("Primary");
                    StartRunningAnimation();
                    break;
                case ConnectState.Waiting:
                    circle.Color = ColorResource.GetResourceColor("Warning");
                    StartWaitingAnimation();
                    break;
                case ConnectState.Error:
                    circle.Color = ColorResource.GetResourceColor("Error");
                    break;
            }
        }

        private async void StartRunningAnimation()
        {
            while (ConnectState == ConnectState.Running)
            {
                await circle.ScaleTo(1.0, 500);
                await circle.ScaleTo(0.9, 500);
            }
        }

        private async void StartWaitingAnimation()
        {
            while (ConnectState == ConnectState.Waiting)
            {
                await circle.ScaleTo(1.0, 2000);
                await

    circle.ScaleTo(0.8, 2000);
            }
        }
    }
}