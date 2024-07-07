using Dissonance.SettingsManagers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.Threading.Tasks;
using System.Windows;

namespace Dissonance
{
	public partial class App : Application
	{
		public static IServiceProvider ServiceProvider { get; private set; }
		private readonly ILogger<App> _logger;

		public App ( )
		{
			var serviceCollection = new ServiceCollection();
			ConfigureServices ( serviceCollection );
			ServiceProvider = serviceCollection.BuildServiceProvider ( );
			_logger = ServiceProvider.GetRequiredService<ILogger<App>> ( );

			_logger.LogInformation ( "Test log entry: Application has started." );
		}

		protected override async void OnStartup ( StartupEventArgs e )
		{
			base.OnStartup ( e );

			try
			{
				await InitializeSettingsAsync ( );
				InitializeMainWindow ( );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred during startup." );
				MessageBox.Show ( "An error occurred during startup. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
				Shutdown ( );
			}
		}

		private void ConfigureServices ( IServiceCollection services )
		{
			services.AddSingleton<ISettingsManager, SettingsManager> ( );
			services.AddSingleton<MainWindow> ( );
			services.AddSingleton<AppSettings> ( ); // Register AppSettings as a singleton
			services.AddLogging ( loggingBuilder => loggingBuilder.ConfigureLogging ( ) );
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
