using System.IO;
using System.Collections.Generic;
using BetcityAnnunciator.Extensions;

namespace BetcityAnnunciator
{
    public class Configuration
    {
        #region Public methods

        public static Configuration FromFile(string path)
        {
            if (!File.Exists(path))
            {
                return new Configuration{ FullPath = path };
            }

            var configuration = path.LoadFromText<Configuration>();
            configuration.FullPath = path;

            return configuration;
        }

        public void SaveAs(string path)
        {
            this.SaveAsText(path);
        }

        public void Save() => SaveAs(FullPath);

        #endregion

        #region Properties

        public string FullPath { get; set; }

        public string Filter { get; set; } = "Теннис";
        public int UpdateInterval { get; set; } = 6;
        public int Delay { get; set; } = 4;

        public bool MuteAll { get; set; }
        public bool MuteBlue { get; set; }
        public bool MuteGreen { get; set; }
        public bool MuteYellow { get; set; }
        public bool MuteOrange { get; set; }

        public bool SortingByColor { get; set; }

        public bool EnabledBlue { get; set; } = true;
        public bool EnabledGreen { get; set; } = true;
        public bool EnabledYellow { get; set; } = true;
        public bool EnabledOrange { get; set; } = true;

        public string RequiredBlueScore { get; set; } = string.Empty;
        public string RequiredGreenScore { get; set; } = string.Empty;
        public string RequiredYellowScore { get; set; } = string.Empty;
        public string RequiredOrangeScore { get; set; } = string.Empty;

        #endregion
    }
}
