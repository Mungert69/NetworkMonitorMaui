using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Controls
{
    public class AnimatedButton : Button
    {
        public AnimatedButton()
        {
            Clicked += async (sender, e) => 
            {
                await this.ScaleToAsync(0.9, 50, Easing.Linear);
                await this.ScaleToAsync(1, 50, Easing.Linear);
            };
        }
    }
}
