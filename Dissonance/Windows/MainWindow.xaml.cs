using System.Text;
using System.Windows;
using Dissonance.SettingsManagers;
using System.Threading.Tasks;

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
			InitializeSettingsAsync ( ).ConfigureAwait ( false );
		}

		private async Task InitializeSettingsAsync ( )
		{
			_appSettings = await _settingsManager.LoadSettingsAsync ( );
			InitializeSettings ( );

			var themeToggleButton = new Dissonance.UserControls.Buttons.ThemeToggleButton
			{
				SettingsManager = _settingsManager,
				AppSettings = _appSettings
			};

			ThemeToggleButtonContainer.Children.Add ( themeToggleButton );
		}

		private void InitializeSettings ( )
		{
			var volume = _appSettings.ScreenReader.Volume;
			var voiceRate = _appSettings.ScreenReader.VoiceRate;
			var zoomLevel = _appSettings.Magnifier.ZoomLevel;
			var invertColors = _appSettings.Magnifier.InvertColors;

			bool isDarkMode = _appSettings.Theme.IsDarkMode;
			ThemeManager.SetTheme ( isDarkMode );
		}

		private async void SaveSettings ( )
		{
			await _settingsManager.SaveSettingsAsync ( _appSettings );
		}
	}
}
