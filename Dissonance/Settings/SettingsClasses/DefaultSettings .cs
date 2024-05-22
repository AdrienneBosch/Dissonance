using Dissonance.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Dissonance.Settings.SettingsClasses
{
    public class DefaultSettings : IDefaultSettings
    {
        public int Volume => 50; // Default volume level

    }
}
