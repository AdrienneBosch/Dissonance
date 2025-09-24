using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace Dissonance.Tests.TestInfrastructure
{
        internal sealed class ListLogger<T> : ILogger<T>, IDisposable
        {
                private sealed class NullScope : IDisposable
                {
                        public static readonly NullScope Instance = new NullScope();

                        public void Dispose()
                        {
                        }
                }

                public List<LogEntry> Entries { get; } = new();

                public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

                public void Dispose()
                {
                }

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
                }
        }

        internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
