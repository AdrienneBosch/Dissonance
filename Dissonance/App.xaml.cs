using Dissonance.SettingsManagers;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading.Tasks;
using System.Windows;

namespace Dissonance
{
	public partial class App : Application
	{
		public static IServiceProvider ServiceProvider { get; private set; }

		protected override async void OnStartup ( StartupEventArgs e )
		{
			base.OnStartup ( e );

			try
			{
				var serviceCollection = new ServiceCollection();
				ConfigureServices ( serviceCollection );
				ServiceProvider = serviceCollection.BuildServiceProvider ( );

				await InitializeSettingsAsync ( );
				InitializeMainWindow ( );
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( "An error occurred during startup. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
				Shutdown ( );
			}
		}

		private void ConfigureServices ( IServiceCollection services )
		{
			services.AddSingleton<ISettingsManager, SettingsManager> ( );
			services.AddSingleton<MainWindow> ( );
			services.AddSingleton<AppSettings> ( ); // Register AppSettings as a singleton
		}

		private async Task InitializeSettingsAsync ( )
		{
			var settingsManager = ServiceProvider.GetRequiredService<ISettingsManager>();
			var appSettings = await settingsManager.LoadSettingsAsync();
			var appSettingsInstance = ServiceProvider.GetRequiredService<AppSettings>();
			appSettingsInstance.CopyFrom ( appSettings );
			ThemeManager.Initialize ( appSettingsInstance ); // Initialize ThemeManager with AppSettings
			ThemeManager.SetTheme ( appSettings.Theme.IsDarkMode );
		}

		private void InitializeMainWindow ( )
		{
			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show ( );
		}
	}
}
