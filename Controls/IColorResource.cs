using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NetworkMonitor.Maui.Controls
{
    public interface IColorResource
    {
        AppTheme GetRequestedTheme();
        Color GetResourceColor(string key);
        Color LightenColor(Color color, float factor);
        void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length);
    }
}
