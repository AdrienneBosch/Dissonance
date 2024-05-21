using Dissonance.AppSettings.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Dissonance.AppSettings.SettingsClasses
{
    public class UserSettings : IUserSettings
    {
        public int Volume { get; set; }

        public void Load(IAppSettings appSettings)
        {
            // Load settings from JSON file or use app settings if not available
            if (File.Exists("userSettings.json"))
            {
                var json = File.ReadAllText("userSettings.json");
                var settings = JsonConvert.DeserializeObject<UserSettings>(json);
                Volume = settings.Volume;
            }
            else
            {
                Volume = appSettings.Volume;
            }
        }

        public void Save()
        {
            // Save settings to JSON file
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText("userSettings.json", json);
        }
    }
}
