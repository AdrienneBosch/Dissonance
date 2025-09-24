using System;

using Dissonance.Services.MessageService;
using Dissonance.Tests.TestInfrastructure;
using Dissonance.ViewModels;

using Microsoft.Extensions.Logging;

namespace Dissonance.Tests.Services
{
        public class MessageServiceTests
        {
                [Fact]
                public void DissonanceMessageBoxShowError_LogsAndDisplays()
                {
                        using var logger = new ListLogger<MessageService>();
                        var service = new MessageService(logger);

                        string? capturedTitle = null;
                        string? capturedMessage = null;
                        bool? capturedShowCancel = null;
                        TimeSpan? capturedDelay = null;

                        try
                        {
                                DissonanceMessageBoxViewModel.ShowOverride = (title, message, showCancel, autoCloseDelay) =>
                                {
                                        capturedTitle = title;
                                        capturedMessage = message;
                                        capturedShowCancel = showCancel;
                                        capturedDelay = autoCloseDelay;
                                        return true;
                                };

                                var exception = new InvalidOperationException("boom");
                                service.DissonanceMessageBoxShowError("Error Title", "Error message", exception);

                                Assert.Equal("Error Title", capturedTitle);
                                Assert.Equal("Error message", capturedMessage);
                                Assert.False(capturedShowCancel);
                                Assert.Null(capturedDelay);

                                Assert.Single(logger.Entries);
                                var entry = logger.Entries[0];
                                Assert.Equal(LogLevel.Error, entry.Level);
                                Assert.Contains("Error message", entry.Message);
                                Assert.Same(exception, entry.Exception);
                        }
                        finally
                        {
                                DissonanceMessageBoxViewModel.ShowOverride = null;
                        }
                }

                [Fact]
                public void DissonanceMessageBoxShowInfo_LogsInformation()
                {
                        using var logger = new ListLogger<MessageService>();
                        var service = new MessageService(logger);

                        string? capturedTitle = null;
                        string? capturedMessage = null;
                        bool? capturedShowCancel = null;
                        TimeSpan? capturedDelay = null;

                        try
                        {
                                DissonanceMessageBoxViewModel.ShowOverride = (title, message, showCancel, autoCloseDelay) =>
                                {
                                        capturedTitle = title;
                                        capturedMessage = message;
                                        capturedShowCancel = showCancel;
                                        capturedDelay = autoCloseDelay;
                                        return true;
                                };

                                var delay = TimeSpan.FromSeconds(5);
                                service.DissonanceMessageBoxShowInfo("Info", "Informational message", delay);

                                Assert.Equal("Info", capturedTitle);
                                Assert.Equal("Informational message", capturedMessage);
                                Assert.False(capturedShowCancel);
                                Assert.Equal(delay, capturedDelay);

                                Assert.Single(logger.Entries);
                                var entry = logger.Entries[0];
                                Assert.Equal(LogLevel.Information, entry.Level);
                                Assert.Contains("Informational message", entry.Message);
                                Assert.Null(entry.Exception);
                        }
                        finally
                        {
                                DissonanceMessageBoxViewModel.ShowOverride = null;
                        }
                }

                [Fact]
                public void DissonanceMessageBoxShowWarning_LogsWarning()
                {
                        using var logger = new ListLogger<MessageService>();
                        var service = new MessageService(logger);

                        try
                        {
                                string? capturedTitle = null;
                                string? capturedMessage = null;
                                bool? capturedShowCancel = null;

                                DissonanceMessageBoxViewModel.ShowOverride = (title, message, showCancel, autoCloseDelay) =>
                                {
                                        capturedTitle = title;
                                        capturedMessage = message;
                                        capturedShowCancel = showCancel;
                                        return true;
                                };

                                service.DissonanceMessageBoxShowWarning("Warning", "Warning message");

                                Assert.Equal("Warning", capturedTitle);
                                Assert.Equal("Warning message", capturedMessage);
                                Assert.False(capturedShowCancel);

                                Assert.Single(logger.Entries);
                                var entry = logger.Entries[0];
                                Assert.Equal(LogLevel.Warning, entry.Level);
                                Assert.Contains("Warning message", entry.Message);
                                Assert.Null(entry.Exception);
                        }
                        finally
                        {
                                DissonanceMessageBoxViewModel.ShowOverride = null;
                        }
                }
        }
}
