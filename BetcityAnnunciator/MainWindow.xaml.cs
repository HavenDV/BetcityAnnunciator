using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
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

        private Timer Timer { get; set; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            RestartTimer();

            Closed += (sender, args) =>
            {
                Settings.Default.Save();

                Cef.Shutdown();
            };
        }

        private void RestartTimer()
        {
            Timer?.Dispose();
            Timer = null;

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

            await Task.Delay(Settings.Default.Delay * 1000);

            var html = await Browser.GetSourceAsync();

            Events = GetEvents(html);
            Events = Events.Where(i => i.Championship.Contains(Settings.Default.Filter)).ToList();

            var requiredGreenScores = Settings.Default.EnabledGreen ? Settings.Default.RequeredGreenScore.Split(';').ToList() : new List<string>();
            var requiredYellowScores = Settings.Default.EnabledYellow ? Settings.Default.RequeredYellowScore.Split(';').ToList() : new List<string>();
            var requiredGrayScores = Settings.Default.EnabledGray ? Settings.Default.RequeredGrayScore.Split(';').ToList() : new List<string>();

            var found = false;
            foreach (var @event in Events)
            {
                found |= @event.ContainsScore(requiredGrayScores, Colors.Gray);
                found |= @event.ContainsScore(requiredYellowScores, Colors.Yellow);
                found |= @event.ContainsScore(requiredGreenScores, Colors.GreenYellow);
            }

            if (!Settings.Default.Mute && found)
            {
                Notify();
            }

            if (Settings.Default.EnabledSorting)
            {
                var sortedList = new List<BetcityEvent>();
                sortedList.AddRange(Events.Where(i => i.Color == Colors.GreenYellow));
                sortedList.AddRange(Events.Where(i => i.Color == Colors.Yellow));
                sortedList.AddRange(Events.Where(i => i.Color == Colors.Gray));
                sortedList.AddRange(Events.Where(i => i.Color == SystemColors.ControlColor));
                Events = sortedList;
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

        private static List<BetcityEvent> GetEvents(HtmlNode node)
        {
            var tooltips = node.SelectNodes("//tooltip[@championship and @score and @title]");
            if (tooltips == null)
            {
                return new List<BetcityEvent>();
            }

            var events = tooltips.Select(i => new BetcityEvent
                {
                    Championship = i.Attributes["championship"]?.Value,
                    RawScore = i.Attributes["score"]?.DeEntitizeValue,
                    Title = i.Attributes["title"]?.Value
                })
                .ToList();

            return events;
        }

        private static void Notify()
        {
            var player = new System.Media.SoundPlayer("beep.wav");
            player.Play();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();

            RestartTimer();
        }
    }
}
