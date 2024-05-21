using Dissonance.AppSettings.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Dissonance.AppSettings.SettingsClasses
{
    public class AppSettings : IAppSettings
    {
        public int Volume { get; set; }

        public void Load()
        {
            // Load settings from JSON file
            var json = File.ReadAllText("appSettings.json");
            var settings = JsonConvert.DeserializeObject<AppSettings>(json);
            Volume = settings.Volume;
        }

        public void Save()
        {
            // Save settings to JSON file
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText("appSettings.json", json);
        }
    }
}
