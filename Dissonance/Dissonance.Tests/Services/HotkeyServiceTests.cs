using System;
using System.Reflection;

using Dissonance.Infrastructure.Constants;
using Dissonance.Services.HotkeyService;
using Dissonance.Tests.TestInfrastructure;

namespace Dissonance.Tests.Services;

public class HotkeyServiceTests
{
        [WindowsFact]
        public void ParseModifiers_ReturnsExpectedFlags()
        {
                StaTestRunner.Run(() =>
                {
                        using var logger = new ListLogger<HotkeyService>();
                        using var service = new HotkeyService(logger, new FakeMessageService());

                        var parseMethod = typeof(HotkeyService).GetMethod("ParseModifiers", BindingFlags.NonPublic | BindingFlags.Instance);
                        Assert.NotNull(parseMethod);

                        var result = (uint)parseMethod!.Invoke(service, new object[] { "Ctrl + Alt" })!;
                        Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, result);

                        result = (uint)parseMethod.Invoke(service, new object[] { "Shift+Win" })!;
                        Assert.Equal(ModifierKeys.Shift | ModifierKeys.Win, result);
                });
        }

        [WindowsFact]
        public void ParseModifiers_ThrowsForInvalidModifiers()
        {
                StaTestRunner.Run(() =>
                {
                        using var logger = new ListLogger<HotkeyService>();
                        using var service = new HotkeyService(logger, new FakeMessageService());

                        var parseMethod = typeof(HotkeyService).GetMethod("ParseModifiers", BindingFlags.NonPublic | BindingFlags.Instance);
                        Assert.NotNull(parseMethod);

                        Assert.Throws<TargetInvocationException>(() => parseMethod!.Invoke(service, new object[] { "Unknown" }));
                        Assert.Throws<TargetInvocationException>(() => parseMethod.Invoke(service, new object[] { string.Empty }));
                });
        }

        [WindowsFact]
        public void WndProc_RaisesHotkeyPressed()
        {
                StaTestRunner.Run(() =>
                {
                        using var logger = new ListLogger<HotkeyService>();
                        using var service = new HotkeyService(logger, new FakeMessageService(), action => action());

                        var invocationCount = 0;
                        service.HotkeyPressed += () => invocationCount++;

                        var wndProc = typeof(HotkeyService).GetMethod("WndProc", BindingFlags.NonPublic | BindingFlags.Instance);
                        Assert.NotNull(wndProc);

                        var args = new object[] { IntPtr.Zero, WindowsMessages.Hotkey, IntPtr.Zero, IntPtr.Zero, false };
                        wndProc!.Invoke(service, args);

                        Assert.Equal(1, invocationCount);
                        Assert.True((bool)args[4]);
                });
        }
}
