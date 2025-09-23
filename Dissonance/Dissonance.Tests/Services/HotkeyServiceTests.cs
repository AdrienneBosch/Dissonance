using System;
using System.Reflection;
using System.Threading;
using System.Windows;

using Dissonance.Infrastructure.Constants;
using Dissonance.Services.HotkeyService;
using Dissonance.Tests.TestInfrastructure;

namespace Dissonance.Tests.Services
{
        public class HotkeyServiceTests
        {
                [Fact]
                public void ParseModifiers_ReturnsExpectedFlags()
                {
                        using var logger = new ListLogger<HotkeyService>();
                        var service = new HotkeyService(logger, new FakeMessageService());

                        var parseMethod = typeof(HotkeyService).GetMethod("ParseModifiers", BindingFlags.NonPublic | BindingFlags.Instance);
                        Assert.NotNull(parseMethod);

                        var result = (uint)parseMethod!.Invoke(service, new object[] { "Ctrl + Alt" })!;
                        Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, result);

                        result = (uint)parseMethod.Invoke(service, new object[] { "Shift+Win" })!;
                        Assert.Equal(ModifierKeys.Shift | ModifierKeys.Win, result);
                }

                [Fact]
                public void ParseModifiers_ThrowsForInvalidModifiers()
                {
                        using var logger = new ListLogger<HotkeyService>();
                        var service = new HotkeyService(logger, new FakeMessageService());

                        var parseMethod = typeof(HotkeyService).GetMethod("ParseModifiers", BindingFlags.NonPublic | BindingFlags.Instance);
                        Assert.NotNull(parseMethod);

                        Assert.Throws<TargetInvocationException>(() => parseMethod!.Invoke(service, new object[] { "Unknown" }));
                        Assert.Throws<TargetInvocationException>(() => parseMethod.Invoke(service, new object[] { string.Empty }));
                }

                [WindowsFact]
                public void WndProc_RaisesHotkeyPressed()
                {
                        StaTestRunner.Run(() =>
                        {
                                using var logger = new ListLogger<HotkeyService>();
                                var service = new HotkeyService(logger, new FakeMessageService());

                                if (Application.Current == null)
                                {
                                        new Application();
                                }

                                var resetEvent = new ManualResetEventSlim();
                                service.HotkeyPressed += () => resetEvent.Set();

                                var wndProc = typeof(HotkeyService).GetMethod("WndProc", BindingFlags.NonPublic | BindingFlags.Instance);
                                Assert.NotNull(wndProc);

                                var args = new object[] { IntPtr.Zero, WindowsMessages.Hotkey, IntPtr.Zero, IntPtr.Zero, false };
                                wndProc!.Invoke(service, args);

                                Application.Current!.Dispatcher.Invoke(() => { });

                                Assert.True(resetEvent.Wait(TimeSpan.FromSeconds(1)));
                                Assert.True((bool)args[4]);
                        });
                }
        }
}
