using System.Windows;

using Dissonance.Infrastructure.Logging.Dissonance.Infrastructure.Logging;
using Dissonance.Services.ClipboardService;
using Dissonance.Services.HotkeyService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;
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
			LoggingConfiguration.Configure ( serviceCollection );
			ConfigureServices ( serviceCollection );
			_serviceProvider = serviceCollection.BuildServiceProvider ( );
		}

		private void ConfigureServices ( IServiceCollection services )
		{
			services.AddSingleton<ISettingsService, SettingsService> ( );
			services.AddSingleton<IClipboardService, ClipboardService> ( );
			services.AddSingleton<ITTSService, TTSService> ( );
			services.AddSingleton<IHotkeyService, HotkeyService> ( );
			services.AddSingleton<MainWindowViewModel> ( );
			services.AddSingleton<MainWindow> ( );
		}

		protected override void OnExit ( ExitEventArgs e )
		{
			if ( _serviceProvider.GetService<IHotkeyService> ( ) is IDisposable hotkeyService )
			{
				hotkeyService.Dispose ( );
			}

			LogManager.Shutdown ( );
			base.OnExit ( e );
		}

		protected override void OnStartup ( StartupEventArgs e )
		{
			try
			{
				var logger = _serviceProvider.GetRequiredService<ILogger<App>>();

				var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
				var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
				var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
				var ttsService = _serviceProvider.GetRequiredService<ITTSService>();
				var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

				mainWindow.Loaded += ( s, ev ) =>
				{
					( ( HotkeyService ) hotkeyService ).Initialize ( mainWindow );

					var settings = settingsService.GetCurrentSettings();
					hotkeyService.RegisterHotkey ( settings.Hotkey.Modifiers, settings.Hotkey.Key );

					ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );

					hotkeyService.HotkeyPressed += ( ) =>
					{
						var clipboardText = clipboardService.GetClipboardText();
						if ( !string.IsNullOrEmpty ( clipboardText ) )
						{
							logger.LogInformation ( $"Hotkey pressed, speaking clipboard text: {clipboardText}" );

							ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
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
			catch ( Exception ex )
			{
				var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
				logger.LogError ( ex, "An error occurred during application startup." );
				throw;
			}
		}
	}
}