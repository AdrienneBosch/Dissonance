using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance
{
    internal class AppSettings
    {
        public double VoiceRate { get; set; }
        public int Volume { get; set; }
        public string Voice { get; set; }
        public HotkeySettings Hotkey { get; set; }

        internal class HotkeySettings
        {
            public string Modifiers { get; set; }
            public string Key { get; set; }
        }
    }
}
