using System;

namespace Dissonance.Services.StatusAnnouncements
{
        public sealed class StatusAnnouncement
        {
                public StatusAnnouncement(string message, StatusSeverity severity)
                {
                        if (string.IsNullOrWhiteSpace(message))
                                throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

                        Message = message;
                        Severity = severity;
                        Timestamp = DateTimeOffset.UtcNow;
                }

                public string Message { get; }

                public StatusSeverity Severity { get; }

                public DateTimeOffset Timestamp { get; }
        }
}
