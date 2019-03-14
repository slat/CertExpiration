using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CertExpiration
{
    public class AppConfig
    {
        public string Urls { get; set; }
        public int? SplitPanelDistance { get; set; }
        public IDictionary<string, int> GridColumnWidths { get; set; } = new Dictionary<string, int>();

        private static string Filename => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appConfig.json");

        public static AppConfig Load()
        {
            if (File.Exists(Filename))
            {
                var text = File.ReadAllText(Filename);
                return JsonConvert.DeserializeObject<AppConfig>(text);
            }
            return new AppConfig();
        }

        public void Save()
        {
            var text = JsonConvert.SerializeObject(this);
            File.WriteAllText(Filename, text);
        }
    }
}
