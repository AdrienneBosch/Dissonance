using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance
{
	public class AppSettings
    {
        public double VoiceRate { get; set; }
        public int Volume { get; set; }
        public string Voice { get; set; }
        public HotkeySettings Hotkey { get; set; }

		public class HotkeySettings
        {
            public string Modifiers { get; set; }
            public string Key { get; set; }
        }
    }
}
