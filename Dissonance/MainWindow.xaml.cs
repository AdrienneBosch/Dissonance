using System.Text;
using System.Windows;

using Dissonance.SetttingsManager;

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
			DisplaySettingsForDebugging ( );
		}

		private void InitializeSettings ( )
		{
			// Example of accessing settings
			var volume = _appSettings.ScreenReader.Volume;
			var voiceRate = _appSettings.ScreenReader.VoiceRate;
			var zoomLevel = _appSettings.Magnifier.ZoomLevel;
			var invertColors = _appSettings.Magnifier.InvertColors;

			// Apply these settings to your application as needed
		}

		private void SaveSettings ( )
		{
			// Modify _appSettings as needed
			_settingsManager.SaveSettings ( _appSettings );
		}

		private void DisplaySettingsForDebugging ( )
		{
			var settingsInfo = new StringBuilder();
			settingsInfo.AppendLine ( "Screen Reader Settings:" );
			settingsInfo.AppendLine ( $"Volume: {_appSettings.ScreenReader.Volume}" );
			settingsInfo.AppendLine ( $"Voice Rate: {_appSettings.ScreenReader.VoiceRate}" );
			settingsInfo.AppendLine ( );
			settingsInfo.AppendLine ( "Magnifier Settings:" );
			settingsInfo.AppendLine ( $"Zoom Level: {_appSettings.Magnifier.ZoomLevel}" );
			settingsInfo.AppendLine ( $"Invert Colors: {_appSettings.Magnifier.InvertColors}" );

			DebugTextBlock.Text = settingsInfo.ToString ( );
		}
	}
}
