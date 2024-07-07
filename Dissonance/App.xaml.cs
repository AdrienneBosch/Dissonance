using Dissonance.SettingsManagers;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Windows;

namespace Dissonance
{
	public partial class App : Application
	{
		public static IServiceProvider ServiceProvider { get; private set; }

		protected override void OnStartup ( StartupEventArgs e )
		{
			base.OnStartup ( e );

			var serviceCollection = new ServiceCollection();
			ConfigureServices ( serviceCollection );
			ServiceProvider = serviceCollection.BuildServiceProvider ( );

			// Retrieve settings and set the theme
			var settingsManager = ServiceProvider.GetRequiredService<ISettingsManager>();
			var appSettings = settingsManager.LoadSettings();
			var appSettingsInstance = ServiceProvider.GetRequiredService<AppSettings>();
			appSettingsInstance.CopyFrom ( appSettings );
			ThemeManager.Initialize ( appSettingsInstance ); // Initialize ThemeManager with AppSettings
			ThemeManager.SetTheme ( appSettings.Theme.IsDarkMode );

			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show ( );
		}

		private void ConfigureServices ( IServiceCollection services )
		{
			services.AddSingleton<ISettingsManager, SettingsManager> ( );
			services.AddSingleton<MainWindow> ( );
			services.AddSingleton<AppSettings> ( ); // Register AppSettings as a singleton
		}
	}
}
