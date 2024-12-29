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

namespace Dissonance
{
	public partial class App : Application
	{
		private readonly IServiceProvider _serviceProvider;
		private bool _isSpeaking = false;

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

		private void DisposeServices ( )
		{
			if ( _serviceProvider.GetService<IHotkeyService> ( ) is IDisposable hotkeyService )
			{
				hotkeyService.Dispose ( );
			}

			LogManager.Shutdown ( );
		}

		private void HandleHotkeyPressed ( ISettingsService settingsService, IClipboardService clipboardService, ITTSService ttsService, ILogger<App> logger )
		{
			if ( _isSpeaking )
			{
				ttsService.Stop ( );
				_isSpeaking = false;
				logger.LogInformation ( "Stopped speaking." );
				return;
			}

			var settings = settingsService.GetCurrentSettings();
			var clipboardText = clipboardService.GetClipboardText();

			if ( !string.IsNullOrEmpty ( clipboardText ) )
			{
				logger.LogInformation ( $"Hotkey pressed, speaking clipboard text: {clipboardText}" );
				ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
				ttsService.Speak ( clipboardText );
				_isSpeaking = true; 
			}
			else
			{
				logger.LogWarning ( "Hotkey pressed, but clipboard is empty or doesn't contain text." );
			}
		}

		private void InitializeApplication ( )
		{
			var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
			logger.LogInformation ( "Application startup." );

			var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
			var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
			var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
			var ttsService = _serviceProvider.GetRequiredService<ITTSService>();
			var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

			mainWindow.Loaded += ( s, ev ) => OnMainWindowLoaded ( mainWindow, hotkeyService, settingsService, clipboardService, ttsService, logger );
			mainWindow.Show ( );
		}

		private void InitializeHotkeyService ( MainWindow mainWindow, IHotkeyService hotkeyService, ISettingsService settingsService, IClipboardService clipboardService, ITTSService ttsService, ILogger<App> logger )
		{
			( ( HotkeyService ) hotkeyService ).Initialize ( mainWindow );

			var settings = settingsService.GetCurrentSettings();
			hotkeyService.RegisterHotkey ( new AppSettings.HotkeySettings
			{
				Modifiers = settings.Hotkey.Modifiers,
				Key = settings.Hotkey.Key
			} );
			ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );

			hotkeyService.HotkeyPressed += ( ) =>
			{
				HandleHotkeyPressed ( settingsService, clipboardService, ttsService, logger );
			};

			logger.LogInformation ( "HotkeyService initialized and hotkey registered." );
		}

		private void OnMainWindowLoaded ( MainWindow mainWindow, IHotkeyService hotkeyService, ISettingsService settingsService, IClipboardService clipboardService, ITTSService ttsService, ILogger<App> logger )
		{
			InitializeHotkeyService ( mainWindow, hotkeyService, settingsService, clipboardService, ttsService, logger );
		}

		protected override void OnExit ( ExitEventArgs e )
		{
			DisposeServices ( );
			base.OnExit ( e );
		}

		protected override void OnStartup ( StartupEventArgs e )
		{
			try
			{
				InitializeApplication ( );
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