using System.ComponentModel;

namespace Dissonance
{
	public class AppSettings : INotifyPropertyChanged
	{
		private ScreenReaderSettings _screenReader;
		private MagnifierSettings _magnifier;
		private ThemeSettings _theme;

		public ScreenReaderSettings ScreenReader
		{
			get => _screenReader;
			set
			{
				_screenReader = value;
				OnPropertyChanged ( nameof ( ScreenReader ) );
			}
		}

		public MagnifierSettings Magnifier
		{
			get => _magnifier;
			set
			{
				_magnifier = value;
				OnPropertyChanged ( nameof ( Magnifier ) );
			}
		}

		public ThemeSettings Theme
		{
			get => _theme;
			set
			{
				_theme = value;
				OnPropertyChanged ( nameof ( Theme ) );
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
		}

		public void CopyFrom ( AppSettings other )
		{
			if ( other == null ) throw new ArgumentNullException ( nameof ( other ) );

			ScreenReader = other.ScreenReader;
			Magnifier = other.Magnifier;
			Theme = other.Theme;
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
		private bool _isDarkMode;

		public bool IsDarkMode
		{
			get => _isDarkMode;
			set
			{
				_isDarkMode = value;
				OnPropertyChanged ( nameof ( IsDarkMode ) );
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
		}
	}
}
