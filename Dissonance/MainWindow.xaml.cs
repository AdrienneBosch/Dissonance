using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Dissonance.SetttingsManager;

namespace Dissonance
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly ISettingsManager _settingsManager;
		private AppSettings _appSettings;

		public MainWindow ( )
		{
			InitializeComponent ( );
			_settingsManager = new SettingsManager ( );
			_appSettings = _settingsManager.LoadSettings ( );
			InitializeSettings ( );
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
	}

}