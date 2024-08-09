using Microsoft.Extensions.Logging;

using Serilog;

namespace Dissonance
{
	public static class LoggerSetup
	{
		public static void ConfigureLogging ( this ILoggingBuilder loggingBuilder )
		{
			var logger = new LoggerConfiguration()
				.WriteTo.Console()
				//.WriteTo.File("Logs/app-.txt", rollingInterval: RollingInterval.Day)
				.CreateLogger();

			loggingBuilder.AddSerilog ( logger );
		}
	}
}
