using Microsoft.Extensions.Logging;

using NLog;
using NLog.Extensions.Logging;

namespace Dissonance
{
	public static class LoggerSetup
	{
		public static void ConfigureLogging ( this ILoggingBuilder loggingBuilder )
		{
			var logger = LogManager.Setup()
				.LoadConfigurationFromFile("nlog.config", optional: false)
				.GetCurrentClassLogger();

			loggingBuilder.ClearProviders ( );
			loggingBuilder.SetMinimumLevel ( Microsoft.Extensions.Logging.LogLevel.Trace );
			loggingBuilder.AddNLog ( );
		}
	}
}
