using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using CefSharp;
using HtmlAgilityPack;
using Timer = System.Timers.Timer;
using BetcityAnnunciator.Properties;

namespace BetcityAnnunciator
{
    public partial class MainWindow
    {
        #region Properties
        
        public List<BetcityEvent> Events { get; set; }
        public Timer Timer { get; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Timer = new Timer(Settings.Default.UpdateInterval * 1000);
            Timer.Elapsed += (sender, args) => Browser.Reload();
            Timer.Start();
        }

        private async void Browser_OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (!e.Frame.IsMain)
            {
                return;
            }

            await Task.Delay(5000);

            var html = await Browser.GetSourceAsync();

            Events = GetEvents(html);

            foreach (var @event in Events)
            {
                @event.Color = @event.Championship.Contains(Settings.Default.Filter) ? Colors.GreenYellow : @event.Color;
            }

            Dispatcher.Invoke(
                () => EventsListBox.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget());
        }

        private static List<BetcityEvent> GetEvents(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return GetEvents(document.DocumentNode);
        }

        private static List<BetcityEvent> GetEvents(HtmlNode node) => node
            .SelectNodes("//tooltip[@championship and @score and @title]")
            .Select(i => new BetcityEvent
            {
                Championship = i.Attributes["championship"].Value,
                Score = i.Attributes["score"].DeEntitizeValue,
                Title = i.Attributes["title"].Value
            })
            .ToList();
    }
}
