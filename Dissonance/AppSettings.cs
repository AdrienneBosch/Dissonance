using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance
{
	public class AppSettings
	{
		public ScreenReaderSettings ScreenReader { get; set; }
		public MagnifierSettings Magnifier { get; set; }
		public ThemeSettings Theme { get; set; }

		public AppSettings ( )
		{
			ScreenReader = new ScreenReaderSettings ( );
			Magnifier = new MagnifierSettings ( );
			Theme = new ThemeSettings ( );
		}
	}

	public class ScreenReaderSettings
	{
		public int Volume { get; set; }
		public int VoiceRate { get; set; }
	}

	public class MagnifierSettings
	{
		public int ZoomLevel { get; set; }
		public bool InvertColors { get; set; }
	}

	public class ThemeSettings
	{
		public bool IsDarkMode { get; set; }
	}
}
