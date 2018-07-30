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

            var requiredBlueScores = Settings.Default.EnabledBlue ? Settings.Default.RequiredBlueScore.Split(';').ToList() : new List<string>();
            var requiredGreenScores = Settings.Default.EnabledGreen ? Settings.Default.RequeredGreenScore.Split(';').ToList() : new List<string>();
            var requiredYellowScores = Settings.Default.EnabledYellow ? Settings.Default.RequeredYellowScore.Split(';').ToList() : new List<string>();
            var requiredOrangeScores = Settings.Default.EnabledOrange ? Settings.Default.RequeredOrangeScore.Split(';').ToList() : new List<string>();

            var foundBlue = false;
            var foundGreen = false;
            var foundYellow = false;
            var foundOrange = false;
            foreach (var @event in Events)
            {
                foundOrange |= !Settings.Default.MuteOrange & @event.ContainsScore(requiredOrangeScores, Colors.Orange);
                foundYellow |= !Settings.Default.MuteYellow & @event.ContainsScore(requiredYellowScores, Colors.Yellow);
                foundGreen |= !Settings.Default.MuteGreen & @event.ContainsScore(requiredGreenScores, Colors.GreenYellow);
                foundBlue |= !Settings.Default.MuteBlue & @event.ContainsScore(requiredBlueScores, Colors.Aqua);
            }

            var found = foundBlue || foundGreen || foundYellow || foundOrange;
            if (!Settings.Default.MuteAll && found)
            {
                Notify();
            }

            if (Settings.Default.SortingByColor)
            {
                var sortedList = new List<BetcityEvent>();
                sortedList.AddRange(Events.Where(i => i.Color == Colors.Aqua));
                sortedList.AddRange(Events.Where(i => i.Color == Colors.GreenYellow));
                sortedList.AddRange(Events.Where(i => i.Color == Colors.Yellow));
                sortedList.AddRange(Events.Where(i => i.Color == Colors.Orange));
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
