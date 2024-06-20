using System.Text;
using System.Windows;

using Dissonance.SettingsManagers;

namespace Dissonance
{
	public partial class MainWindow : Window
	{
		private readonly ISettingsManager _settingsManager;
		private AppSettings _appSettings;

		public MainWindow ( ISettingsManager settingsManager )
		{
			InitializeComponent ( );
			_settingsManager = settingsManager;
			_appSettings = _settingsManager.LoadSettings ( );
			InitializeSettings ( );
		}

		private void InitializeSettings ( )
		{
			var volume = _appSettings.ScreenReader.Volume;
			var voiceRate = _appSettings.ScreenReader.VoiceRate;
			var zoomLevel = _appSettings.Magnifier.ZoomLevel;
			var invertColors = _appSettings.Magnifier.InvertColors;
		}

		private void SaveSettings ( )
		{
			_settingsManager.SaveSettings ( _appSettings );
		}
	}
}
