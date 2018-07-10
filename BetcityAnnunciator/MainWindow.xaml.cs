using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using HtmlAgilityPack;

namespace BetcityAnnunciator
{
    public partial class MainWindow
    {
        #region Properties
        
        private string Data { get; set; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Browser_OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                await Task.Delay(5000);

                //Browser.ViewSource();
                var html = await Browser.GetSourceAsync();

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var champs = document.DocumentNode
                    .SelectNodes("//div[@class=\"live-results-championship ng-scope\"]");

                var names = champs
                    .SelectMany(i => i.SelectNodes("//div[@class=\"live-results-championship-header__name\"]"))
                    .Select(i => i.InnerHtml);

                var text = string.Join(Environment.NewLine, names);
                File.WriteAllText("D:/test.txt", text);
                MessageBox.Show(text);
            }
        }
    }
}
