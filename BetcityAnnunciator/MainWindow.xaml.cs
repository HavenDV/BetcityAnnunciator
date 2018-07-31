using System;
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
            return node
                       .SelectNodes("//tooltip[@championship and @score and @title]")
                       ?.Select(CreateEventFromTooltip)
                       .ToList() ?? new List<BetcityEvent>();
        }

        private static BetcityEvent CreateEventFromTooltip(HtmlNode node)
        {
            //["1:1"," (6:4, 4:6, 0:1)"]
            //["69:72"," (20:17, 30:23, 19:23, 0:9)"]
            // ? ["69:72"," (20:17, 30:23, 19:23, 0:9) (матч приостановлен)"]
            var rawScore = node.Attributes["score"]?.DeEntitizeValue ?? string.Empty;
            rawScore = rawScore.Trim('[', ']');

            var splitByQuotes = rawScore.Split(new []{ '\"' }, StringSplitOptions.RemoveEmptyEntries);
            var mainScore = splitByQuotes.FirstOrDefault() ?? string.Empty;
            var additionalString = splitByQuotes.LastOrDefault()?.Trim() ?? string.Empty;

            var splitByBrackets = additionalString.Split(new[] { ')', '(' }, StringSplitOptions.RemoveEmptyEntries);
            var additionalScore = splitByBrackets.FirstOrDefault() ?? string.Empty;
            var additionalInfo = splitByBrackets.ElementAtOrDefault(2) ?? string.Empty;
            var setScores = additionalScore.Split(',').Select(i => i.Trim()).ToList();

            return new BetcityEvent
            {
                Championship = node.Attributes["championship"]?.Value,
                Title = node.Attributes["title"]?.Value,
                MainScore = mainScore,
                SetScores = setScores,
                AdditionalInfo = additionalInfo
            };
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
