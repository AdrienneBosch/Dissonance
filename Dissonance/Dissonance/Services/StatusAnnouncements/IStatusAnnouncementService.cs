using System;
using System.Collections.Generic;

namespace Dissonance.Services.StatusAnnouncements
{
        public interface IStatusAnnouncementService
        {
                StatusAnnouncement? Latest { get; }

                IReadOnlyList<StatusAnnouncement> History { get; }

                event EventHandler<StatusAnnouncement>? StatusAnnounced;

                void Announce(StatusAnnouncement announcement);

                void Announce(string message, StatusSeverity severity = StatusSeverity.Info);

                void AnnounceFromResource(string resourceKey, string fallbackMessage, StatusSeverity severity = StatusSeverity.Info);
        }
}
