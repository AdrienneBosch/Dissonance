using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

namespace Dissonance.Infrastructure.Logging
{
	namespace Dissonance.Infrastructure.Logging
	{
		public static class LoggingConfiguration
		{
			public static void Configure ( IServiceCollection services )
			{
				var loggerConfig = new NLog.Config.LoggingConfiguration();

				var fileTarget = new NLog.Targets.FileTarget("logfile")
				{
					FileName = "app_logs.txt",
					Layout = "${longdate} ${uppercase:${level}} ${message} ${exception:format=ToString}"
				};

				loggerConfig.AddTarget ( fileTarget );
				loggerConfig.AddRule ( NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget );

				NLog.LogManager.Configuration = loggerConfig;

				services.AddLogging ( loggingBuilder =>
				{
					loggingBuilder.ClearProviders ( );
					loggingBuilder.SetMinimumLevel ( Microsoft.Extensions.Logging.LogLevel.Debug ); 
					loggingBuilder.AddNLog ( ); 
				} );
			}
		}
	}
}
