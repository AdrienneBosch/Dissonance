using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace Dissonance
{
	public static class LoggerSetup
	{
		public static void ConfigureLogging ( this ILoggingBuilder loggingBuilder )
		{
			var logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File("Logs/app-.txt", rollingInterval: RollingInterval.Day)
				.CreateLogger();

			loggingBuilder.AddSerilog ( logger );
		}
	}
}
