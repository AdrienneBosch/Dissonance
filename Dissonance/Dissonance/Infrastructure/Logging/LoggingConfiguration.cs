using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace Dissonance.Infrastructure.Logging
{
        public static class LoggingConfiguration
        {
                public static void Configure ( IServiceCollection services )
                {
                        var loggerConfig = new LoggingConfiguration ( );

                        var fileTarget = new FileTarget ( "logfile" )
                        {
                                FileName = "app_logs.txt",
                                Layout = "${longdate} ${uppercase:${level}} ${logger} ${message} ${exception:format=ToString}"
                        };

                        loggerConfig.AddTarget ( fileTarget );
                        loggerConfig.AddRule ( NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget );

                        NLog.LogManager.Configuration = loggerConfig;

                        services.AddLogging ( loggingBuilder =>
                        {
                                loggingBuilder.ClearProviders ( );
                                loggingBuilder.SetMinimumLevel ( LogLevel.Debug );
                                loggingBuilder.AddNLog ( );
                        } );
                }
        }
}