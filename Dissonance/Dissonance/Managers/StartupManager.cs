using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
	public class StartupManager : IDisposable
	{
		private readonly ILogger<StartupManager> _logger;
		private readonly IServiceProvider _serviceProvider;
		private bool _disposed = false;

		public StartupManager ( IServiceProvider serviceProvider, ILogger<StartupManager> logger )
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException ( nameof ( serviceProvider ) );
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
		}

		public void Dispose ( )
		{
			if ( _disposed ) return;

			_logger.LogInformation ( "Disposing resources..." );
			var disposableServices = _serviceProvider.GetServices<IDisposable>();
			foreach ( var service in disposableServices )
			{
				service.Dispose ( );
			}

			_disposed = true;
			_logger.LogInformation ( "Resources disposed." );
		}

		public void Initialize ( MainWindow mainWindow )
		{
			if ( mainWindow == null ) throw new ArgumentNullException ( nameof ( mainWindow ) );

                        mainWindow.Loaded += ( s, e ) =>
                        {
                                var clipboardManager = _serviceProvider.GetRequiredService<ClipboardManager>();
                                clipboardManager.Initialize ( mainWindow );

                                var hotkeyManager = _serviceProvider.GetRequiredService<HotkeyManager>();
                                hotkeyManager.Initialize ( mainWindow );
                        };

			_logger.LogInformation ( "StartupManager subscribed to MainWindow Loaded event." );
		}
	}
}