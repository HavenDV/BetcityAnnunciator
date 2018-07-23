using System.Windows;
using System.Windows.Media;

namespace BetcityAnnunciator
{
    public class BetcityEvent
    {
        public string Championship { get; set; }
        public string Title { get; set; }
        public string Score { get; set; }

        public Color Color { get; set; } = SystemColors.ControlColor;
    }
}