using System;
using System.Collections.Generic;

using Dissonance.Services.MessageService;

namespace Dissonance.Tests.TestInfrastructure
{
        internal sealed class FakeMessageService : IMessageService
        {
                public List<(string Title, string Message, Exception? Exception)> Errors { get; } = new();
                public List<(string Title, string Message, TimeSpan? AutoCloseDelay)> Infos { get; } = new();
                public List<(string Title, string Message)> Warnings { get; } = new();

                public void DissonanceMessageBoxShowError(string title, string message, Exception ex = null)
                {
                        Errors.Add((title, message, ex));
                }

                public void DissonanceMessageBoxShowInfo(string title, string message, TimeSpan? autoCloseDelay = null)
                {
                        Infos.Add((title, message, autoCloseDelay));
                }

                public void DissonanceMessageBoxShowWarning(string title, string message)
                {
                        Warnings.Add((title, message));
                }
        }
}
