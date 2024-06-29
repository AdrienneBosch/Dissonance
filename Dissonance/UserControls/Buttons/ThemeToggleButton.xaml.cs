using Dissonance.SettingsManagers;
using System.Windows.Controls;

namespace Dissonance.UserControls.Buttons
{
	/// <summary>
	/// Interaction logic for ThemeToggleButton.xaml
	/// </summary>
	public partial class ThemeToggleButton : UserControl
	{
		private ISettingsManager _settingsManager;
		private AppSettings _appSettings;

		public ThemeToggleButton ( )
		{
			InitializeComponent ( );
		}

		public ISettingsManager SettingsManager
		{
			get { return _settingsManager; }
			set
			{
				_settingsManager = value;
				// Additional initialization if needed
			}
		}

		public AppSettings AppSettings
		{
			get { return _appSettings; }
			set
			{
				_appSettings = value;
				// Additional initialization if needed
			}
		}
	}
}
