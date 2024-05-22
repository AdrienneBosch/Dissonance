using Dissonance.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Dissonance.Settings.SettingsClasses
{
    public class SettingsManager : ISettingsManager
    {
        public readonly IAppSettings _appSettings;
        public readonly IUserSettings _userSettings;

        public SettingsManager (IAppSettings appSettings, IUserSettings userSettings)
        {
            _appSettings = appSettings;
            _userSettings = userSettings;
        }

        public void LoadSettings()
        { 
            _appSettings.Load();
            _userSettings.Load(_appSettings);
        }

        public void SaveUserSettings()
        {
            _userSettings.Save();
        }

    }
}
