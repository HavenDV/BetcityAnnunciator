using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace BetcityAnnunciator
{
    public class BetcityEvent
    {
        public string Championship { get; set; }
        public string Title { get; set; }
        public string RawScore { get; set; } //["69:72"," (20:17, 30:23, 19:23, 0:9)"]
        public string MainScore => RawScore.Split(',').FirstOrDefault()?.Trim('\"', '[') ?? string.Empty;
        public string AdditionalScore => RawScore.Replace(MainScore, string.Empty);

        public Color Color { get; set; } = SystemColors.ControlColor;
    }
}