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

			_logger.LogInformation ( "Application has started." );
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
			services.AddTransient<ISettingsManager, SettingsManager> ( );
			services.AddTransient<MainWindow> ( );
			services.AddSingleton<AppSettings> ( );
			services.AddLogging ( loggingBuilder => loggingBuilder.ConfigureLogging ( ) );
		}

		private async Task InitializeSettingsAsync ( )
		{
			try
			{
				var settingsManager = ServiceProvider.GetRequiredService<ISettingsManager>();
				var appSettings = await settingsManager.LoadSettingsAsync();
				var appSettingsInstance = ServiceProvider.GetRequiredService<AppSettings>();
				appSettingsInstance.CopyFrom ( appSettings );
				ThemeManager.Initialize ( appSettingsInstance );
				ThemeManager.SetTheme ( appSettings.Theme.IsDarkMode );

				_logger.LogInformation ( "Settings initialized successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred during settings initialization." );
				throw;
			}
		}

		private void InitializeMainWindow ( )
		{
			try
			{
				var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
				mainWindow.Show ( );

				_logger.LogInformation ( "Main window initialized and displayed successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while initializing the main window." );
				throw;
			}
		}
	}
}
