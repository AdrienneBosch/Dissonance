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

		public AppSettings ( )
		{
			ScreenReader = new ScreenReaderSettings ( );
			Magnifier = new MagnifierSettings ( );
		}
	}

	public class ScreenReaderSettings
	{
		public int Volume { get; set; }
		public int VoiceRate { get; set; }
		// Add other screen reader settings as needed
	}

	public class MagnifierSettings
	{
		public int ZoomLevel { get; set; }
		public bool InvertColors { get; set; }
		// Add other magnifier settings as needed
	}

}
