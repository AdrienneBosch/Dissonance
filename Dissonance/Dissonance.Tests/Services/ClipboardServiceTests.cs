using System.Windows;

using Dissonance.Services.ClipboardService;
using Dissonance.Tests.TestInfrastructure;

using Microsoft.Extensions.Logging;

namespace Dissonance.Tests.Services
{
        public class ClipboardServiceTests
        {
                [WindowsFact]
                public void GetClipboardText_ReturnsClipboardContents_WhenAvailable()
                {
                        StaTestRunner.Run(() =>
                        {
                                Clipboard.Clear();
                                var expected = "Clipboard message";
                                Clipboard.SetText(expected);

                                using var logger = new ListLogger<ClipboardService>();
                                var service = new ClipboardService(logger);

                                var text = service.GetClipboardText();

                                Assert.Equal(expected, text);
                                Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("Clipboard text retrieved."));
                        });
                }

                [WindowsFact]
                public void GetClipboardText_ReturnsNull_WhenClipboardEmpty()
                {
                        StaTestRunner.Run(() =>
                        {
                                Clipboard.Clear();

                                using var logger = new ListLogger<ClipboardService>();
                                var service = new ClipboardService(logger);

                                var text = service.GetClipboardText();

                                Assert.Null(text);
                                Assert.Empty(logger.Entries);
                        });
                }

                [WindowsFact]
                public void IsTextAvailable_ReflectsClipboardState()
                {
                        StaTestRunner.Run(() =>
                        {
                                Clipboard.Clear();
                                using var logger = new ListLogger<ClipboardService>();
                                var service = new ClipboardService(logger);

                                Assert.False(service.IsTextAvailable());

                                Clipboard.SetText("value");
                                Assert.True(service.IsTextAvailable());
                        });
                }
        }
}
