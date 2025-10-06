using System;
using System.Windows;

using Dissonance.Infrastructure.Logging;
using Dissonance.Managers;
using Dissonance.Services.ClipboardService;
using Dissonance.Services.DocumentReader;
using Dissonance.Services.DocumentReaderHotkeyService;
using Dissonance.Services.HotkeyService;
using Dissonance.Services.MessageService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.StatusAnnouncements;
using Dissonance.Services.ThemeService;
using Dissonance.Services.TTSService;
using Dissonance.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dissonance
{
        public partial class App : Application
        {
                private readonly IServiceProvider _serviceProvider;
                private StartupManager? _startupManager;

                public App ( )
                {
                        var serviceCollection = new ServiceCollection ( );
                        LoggingConfiguration.Configure ( serviceCollection );
                        ConfigureServices ( serviceCollection );
                        _serviceProvider = serviceCollection.BuildServiceProvider ( );
                }

                private void ConfigureServices ( IServiceCollection services )
                {
                        services.AddSingleton<ISettingsService, SettingsService> ( );
                        services.AddSingleton<IClipboardService, ClipboardService> ( );
                        services.AddSingleton<IDocumentReaderService, DocumentReaderService> ( );
                        services.AddSingleton<ITTSService, TTSService> ( );
                        services.AddSingleton<IThemeService, ThemeService> ( );
                        services.AddSingleton<IStatusAnnouncementService, StatusAnnouncementService> ( );
                        services.AddSingleton<IHotkeyService, HotkeyService> ( );
                        services.AddSingleton<IDocumentReaderHotkeyService, DocumentReaderHotkeyService> ( );
                        services.AddSingleton<IMessageService, MessageService> ( );
                        services.AddSingleton<ClipboardManager> ( );
                        services.AddSingleton<HotkeyManager> ( );
                        services.AddSingleton<DocumentReaderHotkeyManager> ( );
                        services.AddSingleton<StartupManager> ( );
                        services.AddSingleton<DocumentReaderViewModel> ( );
                        services.AddSingleton<MainWindowViewModel> ( );
                        services.AddSingleton<MainWindow> ( );
                }

                protected override void OnExit ( ExitEventArgs e )
                {
                        _startupManager?.Dispose ( );
                        base.OnExit ( e );
                }

                protected override void OnStartup ( StartupEventArgs e )
                {
                        base.OnStartup ( e );

                        var logger = _serviceProvider.GetRequiredService<ILogger<App>> ( );
                        var settingsService = _serviceProvider.GetRequiredService<ISettingsService> ( );
                        var themeService = _serviceProvider.GetRequiredService<IThemeService> ( );
                        var settings = settingsService.GetCurrentSettings ( );
                        var theme = settings.UseDarkTheme ? AppTheme.Dark : AppTheme.Light;
                        themeService.ApplyTheme ( theme );
                        settings.UseDarkTheme = theme == AppTheme.Dark;
                        _startupManager = _serviceProvider.GetRequiredService<StartupManager> ( );

                        try
                        {
                                var mainWindow = _serviceProvider.GetRequiredService<MainWindow> ( );
                                _startupManager.Initialize ( mainWindow );
                                mainWindow.Show ( );
                                logger.LogInformation ( "Application started." );
                        }
                        catch ( Exception ex )
                        {
                                logger.LogError ( ex, "Application startup failed." );
                                throw;
                        }
                }
        }
}
