using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CefSharp;
using HtmlAgilityPack;

namespace BetcityAnnunciator
{
    public partial class MainWindow
    {
        #region Properties
        
        private List<BetcityEvent> Events { get; set; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
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
