using System;
using System.Windows;
using Dissonance.Services.ClipboardService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;
using Dissonance.Services.HotkeyService;
using Dissonance.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Dissonance
{
	public partial class App : Application
	{
		private readonly IServiceProvider _serviceProvider;

		public App ( )
		{
			var serviceCollection = new ServiceCollection();
			ConfigureLogging ( serviceCollection );  // Configure logging
			ConfigureServices ( serviceCollection );
			_serviceProvider = serviceCollection.BuildServiceProvider ( );
		}

		private void ConfigureLogging ( IServiceCollection services )
		{
			var loggerConfig = new NLog.Config.LoggingConfiguration();
			var fileTarget = new NLog.Targets.FileTarget("logfile") { FileName = "app_logs.txt" };
			loggerConfig.AddTarget ( fileTarget );
			loggerConfig.AddRule ( NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget );
			NLog.LogManager.Configuration = loggerConfig;

			services.AddLogging ( loggingBuilder =>
			{
				loggingBuilder.ClearProviders ( );
				loggingBuilder.SetMinimumLevel ( Microsoft.Extensions.Logging.LogLevel.Trace );
				loggingBuilder.AddNLog ( );
			} );
		}

		private void ConfigureServices ( IServiceCollection services )
		{
			services.AddSingleton<ISettingsService, SettingsService> ( );
			services.AddSingleton<IClipboardService, ClipboardService> ( );
			services.AddSingleton<ITTSService, TTSService> ( );
			services.AddSingleton<IHotkeyService, HotkeyService> ( );  // Register HotkeyService
			services.AddSingleton<MainWindowViewModel> ( );
			services.AddSingleton<MainWindow> ( );
		}

		protected override void OnStartup ( StartupEventArgs e )
		{
			var logger = _serviceProvider.GetRequiredService<ILogger<App>>();

			var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
			var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
			var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
			var ttsService = _serviceProvider.GetRequiredService<ITTSService>();
			var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

			// Hook into the MainWindow's Loaded event to initialize the HotkeyService when the window is fully loaded.
			mainWindow.Loaded += ( s, ev ) =>
			{
				( ( HotkeyService ) hotkeyService ).Initialize ( mainWindow );

				// Register the hotkey using the settings from SettingsService
				var settings = settingsService.GetCurrentSettings();
				hotkeyService.RegisterHotkey ( settings.Hotkey.Modifiers, settings.Hotkey.Key );

				hotkeyService.HotkeyPressed += ( ) =>
				{
					var clipboardText = clipboardService.GetClipboardText();
					if ( !string.IsNullOrEmpty ( clipboardText ) )
					{
						logger.LogInformation ( $"Hotkey pressed, speaking clipboard text: {clipboardText}" );
						ttsService.Speak ( clipboardText );
					}
					else
					{
						logger.LogWarning ( "Hotkey pressed, but clipboard is empty or doesn't contain text." );
					}
				};

				logger.LogInformation ( "HotkeyService initialized and hotkey registered." );
			};

			logger.LogInformation ( "Application startup." );
			mainWindow.Show ( );
		}

		protected override void OnExit ( ExitEventArgs e )
		{
			LogManager.Shutdown ( );
			base.OnExit ( e );
		}
	}
}
