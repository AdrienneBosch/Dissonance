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
using Dissonance.Settings.Interfaces;

namespace Dissonance
{
    public partial class MainWindow : Window
    {
        private readonly ISettingsManager _settingsManager;

        public MainWindow(ISettingsManager settingsManager)
        {
            InitializeComponent();
            _settingsManager = settingsManager;

            // Load settings
            _settingsManager.LoadSettings();
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            _settingsManager.SaveUserSettings();
        }
    }
}
