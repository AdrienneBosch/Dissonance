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

		public void CopyFrom ( AppSettings other )
		{
			if ( other == null ) throw new ArgumentNullException ( nameof ( other ) );

			ScreenReader.Volume = other.ScreenReader.Volume;
			ScreenReader.VoiceRate = other.ScreenReader.VoiceRate;
			Magnifier.ZoomLevel = other.Magnifier.ZoomLevel;
			Magnifier.InvertColors = other.Magnifier.InvertColors;
			Theme.IsDarkMode = other.Theme.IsDarkMode;
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
