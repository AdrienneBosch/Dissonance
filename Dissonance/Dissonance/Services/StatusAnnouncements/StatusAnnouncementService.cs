using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Dissonance.Services.StatusAnnouncements
{
        internal sealed class StatusAnnouncementService : IStatusAnnouncementService
        {
                private const int MaxHistory = 100;
                private readonly List<StatusAnnouncement> _history = new List<StatusAnnouncement>();
                private readonly object _syncRoot = new object();

                public event EventHandler<StatusAnnouncement>? StatusAnnounced;

                public StatusAnnouncement? Latest { get; private set; }

                public IReadOnlyList<StatusAnnouncement> History
                {
                        get
                        {
                                lock (_syncRoot)
                                {
                                        return _history.ToList().AsReadOnly();
                                }
                        }
                }

                public void Announce(StatusAnnouncement announcement)
                {
                        if (announcement == null)
                                throw new ArgumentNullException(nameof(announcement));

                        lock (_syncRoot)
                        {
                                _history.Add(announcement);
                                if (_history.Count > MaxHistory)
                                {
                                        _history.RemoveRange(0, _history.Count - MaxHistory);
                                }

                                Latest = announcement;
                        }

                        StatusAnnounced?.Invoke(this, announcement);
                }

                public void Announce(string message, StatusSeverity severity = StatusSeverity.Info)
                {
                        Announce(new StatusAnnouncement(message, severity));
                }

                public void AnnounceFromResource(string resourceKey, string fallbackMessage, StatusSeverity severity = StatusSeverity.Info)
                {
                        var message = TryGetResourceString(resourceKey, fallbackMessage);
                        Announce(message, severity);
                }

                private static string TryGetResourceString(string resourceKey, string fallbackMessage)
                {
                        if (Application.Current?.TryFindResource(resourceKey) is string resourceValue && !string.IsNullOrWhiteSpace(resourceValue))
                                return resourceValue;

                        return fallbackMessage;
                }
        }
}
