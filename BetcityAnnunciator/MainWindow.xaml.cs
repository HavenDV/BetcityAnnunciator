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

            var requiredMainScores = Settings.Default.RequestMainScore.Split(';').ToList();
            var found = false;
            foreach (var @event in Events)
            {
                if (!@event.Championship.Contains(Settings.Default.Filter))
                {
                    continue;
                }

                @event.Color =  Colors.GreenYellow;

                if (requiredMainScores.Any())
                {
                    foreach (var requiredScore in requiredMainScores)
                    {
                        if (!@event.MainScore.Contains(requiredScore))
                        {
                            continue;
                        }

                        found = true;
                        @event.Color = Colors.Red;
                    }
                }
            }

            if (found)
            {
                Notify();
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
                RawScore = i.Attributes["score"].DeEntitizeValue,
                Title = i.Attributes["title"].Value
            })
            .ToList();

        private static void Notify()
        {
            var player = new System.Media.SoundPlayer("beep.wav");
            player.Play();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
