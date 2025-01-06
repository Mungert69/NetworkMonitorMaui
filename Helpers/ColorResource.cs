namespace NetworkMonitor.Maui;
public static class ColorResource
{

    public static AppTheme GetRequestedTheme()
    {

        try
        {
            if (Application.Current != null && Application.Current.RequestedTheme != null)
            {
                return Application.Current.RequestedTheme;
            }
        }
        catch{}
         return AppTheme.Light;
    }
    public static Color GetResourceColor(string key)
    {
        try
        {
            if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var colorValue))
            {
                return (Color)colorValue;
            }
        }
        catch{}
         return Colors.Transparent;
    }

    public static Color LightenColor(Color color, float factor)
    {
        factor = Math.Max(0, factor); // Ensure factor is non-negative

        return new Color(
            Math.Min(color.Red + factor, 1.0f),
            Math.Min(color.Green + factor, 1.0f),
            Math.Min(color.Blue + factor, 1.0f));
    }

    public static void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length)
    {
        if (boxView == null) throw new ArgumentNullException(nameof(boxView));
        if (fromColor == null) throw new ArgumentNullException(nameof(fromColor));
        if (toColor == null) throw new ArgumentNullException(nameof(toColor));

        var animation = new Animation(v =>
        {
            var color = Color.FromRgb(
                lerp(fromColor.Red, toColor.Red, v),
                lerp(fromColor.Green, toColor.Green, v),
                lerp(fromColor.Blue, toColor.Blue, v));
            boxView.Color = color;
        }, 0, 1);

        animation.Commit(boxView, "ColorChange", length: length);
    }

    private static double lerp(double start, double end, double t)
    {
        return start + (end - start) * t;
    }




}
