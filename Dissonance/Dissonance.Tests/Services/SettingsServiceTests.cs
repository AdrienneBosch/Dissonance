using System;
using System.IO;

using Dissonance;
using Dissonance.Services.SettingsService;
using Dissonance.Tests.TestInfrastructure;

using Microsoft.Extensions.Logging.Abstractions;

using Newtonsoft.Json;

namespace Dissonance.Tests.Services
{
        public class SettingsServiceTests
        {
                [Fact]
                public void LoadSettings_CreatesFileWithDefaults_WhenFileMissing()
                {
                        var messageService = new FakeMessageService();

                        using var scope = new TestDirectoryScope();
                        var service = CreateService(messageService);

                        var settings = service.GetCurrentSettings();

                        Assert.Equal("Microsoft David", settings.Voice);
                        Assert.Equal(1.0, settings.VoiceRate);
                        Assert.Equal(50, settings.Volume);
                        Assert.False(settings.SaveConfigAsDefaultOnClose);
                        Assert.False(settings.UseDarkTheme);
                        Assert.Null(settings.WindowLeft);
                        Assert.Null(settings.WindowTop);
                        Assert.Null(settings.WindowWidth);
                        Assert.Null(settings.WindowHeight);
                        Assert.False(settings.IsWindowMaximized);
                        Assert.Equal("Alt", settings.Hotkey.Modifiers);
                        Assert.Equal("E", settings.Hotkey.Key);

                        var settingsFile = Path.Combine(scope.DirectoryPath, "appsettings.json");
                        Assert.True(File.Exists(settingsFile));

                        var storedSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(settingsFile));
                        Assert.NotNull(storedSettings);
                        Assert.Equal(settings.Voice, storedSettings!.Voice);
                        Assert.Equal(settings.VoiceRate, storedSettings.VoiceRate);
                        Assert.Equal(settings.Volume, storedSettings.Volume);
                        Assert.Equal(settings.SaveConfigAsDefaultOnClose, storedSettings.SaveConfigAsDefaultOnClose);
                        Assert.Equal(settings.UseDarkTheme, storedSettings.UseDarkTheme);
                        Assert.Equal(settings.WindowLeft, storedSettings.WindowLeft);
                        Assert.Equal(settings.WindowTop, storedSettings.WindowTop);
                        Assert.Equal(settings.WindowWidth, storedSettings.WindowWidth);
                        Assert.Equal(settings.WindowHeight, storedSettings.WindowHeight);
                        Assert.Equal(settings.IsWindowMaximized, storedSettings.IsWindowMaximized);
                        Assert.Equal(settings.Hotkey.Modifiers, storedSettings.Hotkey!.Modifiers);
                        Assert.Equal(settings.Hotkey.Key, storedSettings.Hotkey.Key);

                        Assert.Empty(messageService.Errors);
                        Assert.Empty(messageService.Warnings);
                }

                [Fact]
                public void SaveSettings_NormalizesInvalidValuesAndPersists()
                {
                        var messageService = new FakeMessageService();

                        using var scope = new TestDirectoryScope();
                        var service = CreateService(messageService);

                        var invalidSettings = new AppSettings
                        {
                                Voice = string.Empty,
                                VoiceRate = -5,
                                Volume = 500,
                                SaveConfigAsDefaultOnClose = true,
                                UseDarkTheme = true,
                                WindowLeft = double.NaN,
                                WindowTop = double.PositiveInfinity,
                                WindowWidth = -10,
                                WindowHeight = 0,
                                IsWindowMaximized = true,
                                Hotkey = new AppSettings.HotkeySettings
                                {
                                        Modifiers = string.Empty,
                                        Key = string.Empty,
                                }
                        };

                        service.SaveSettings(invalidSettings);

                        var current = service.GetCurrentSettings();

                        Assert.Equal("Microsoft David", current.Voice);
                        Assert.Equal(1.0, current.VoiceRate);
                        Assert.Equal(50, current.Volume);
                        Assert.True(current.SaveConfigAsDefaultOnClose);
                        Assert.True(current.UseDarkTheme);
                        Assert.Null(current.WindowLeft);
                        Assert.Null(current.WindowTop);
                        Assert.Null(current.WindowWidth);
                        Assert.Null(current.WindowHeight);
                        Assert.True(current.IsWindowMaximized);
                        Assert.Equal("Alt", current.Hotkey.Modifiers);
                        Assert.Equal("E", current.Hotkey.Key);

                        var settingsFile = Path.Combine(scope.DirectoryPath, "appsettings.json");
                        var storedSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(settingsFile));
                        Assert.NotNull(storedSettings);
                        Assert.Equal(current.Voice, storedSettings!.Voice);
                        Assert.Equal(current.VoiceRate, storedSettings.VoiceRate);
                        Assert.Equal(current.Volume, storedSettings.Volume);
                        Assert.Equal(current.SaveConfigAsDefaultOnClose, storedSettings.SaveConfigAsDefaultOnClose);
                        Assert.Equal(current.UseDarkTheme, storedSettings.UseDarkTheme);
                        Assert.Equal(current.WindowLeft, storedSettings.WindowLeft);
                        Assert.Equal(current.WindowTop, storedSettings.WindowTop);
                        Assert.Equal(current.WindowWidth, storedSettings.WindowWidth);
                        Assert.Equal(current.WindowHeight, storedSettings.WindowHeight);
                        Assert.Equal(current.IsWindowMaximized, storedSettings.IsWindowMaximized);
                        Assert.Equal(current.Hotkey.Modifiers, storedSettings.Hotkey!.Modifiers);
                        Assert.Equal(current.Hotkey.Key, storedSettings.Hotkey.Key);

                        Assert.Empty(messageService.Errors);
                }

                [Fact]
                public void SaveCurrentSettingsAsDefault_PersistsDefaultAndCurrentCopies()
                {
                        var messageService = new FakeMessageService();

                        using var scope = new TestDirectoryScope();
                        var service = CreateService(messageService);

                        var desiredSettings = new AppSettings
                        {
                                Voice = "Custom Voice",
                                VoiceRate = 1.25,
                                Volume = 80,
                                SaveConfigAsDefaultOnClose = true,
                                UseDarkTheme = true,
                                WindowLeft = 200,
                                WindowTop = 300,
                                WindowWidth = 1024,
                                WindowHeight = 768,
                                IsWindowMaximized = true,
                                Hotkey = new AppSettings.HotkeySettings
                                {
                                        Modifiers = "Ctrl+Shift",
                                        Key = "H",
                                }
                        };

                        service.SaveSettings(desiredSettings);

                        var success = service.SaveCurrentSettingsAsDefault();
                        Assert.True(success);

                        var current = service.GetCurrentSettings();
                        Assert.Equal(desiredSettings.Voice, current.Voice);
                        Assert.Equal(desiredSettings.VoiceRate, current.VoiceRate);
                        Assert.Equal(desiredSettings.Volume, current.Volume);
                        Assert.Equal(desiredSettings.SaveConfigAsDefaultOnClose, current.SaveConfigAsDefaultOnClose);
                        Assert.Equal(desiredSettings.UseDarkTheme, current.UseDarkTheme);
                        Assert.Equal(desiredSettings.WindowLeft, current.WindowLeft);
                        Assert.Equal(desiredSettings.WindowTop, current.WindowTop);
                        Assert.Equal(desiredSettings.WindowWidth, current.WindowWidth);
                        Assert.Equal(desiredSettings.WindowHeight, current.WindowHeight);
                        Assert.Equal(desiredSettings.IsWindowMaximized, current.IsWindowMaximized);
                        Assert.Equal(desiredSettings.Hotkey!.Modifiers, current.Hotkey.Modifiers);
                        Assert.Equal(desiredSettings.Hotkey.Key, current.Hotkey.Key);

                        var defaultFile = Path.Combine(scope.DirectoryPath, "appsettings.default.json");
                        Assert.True(File.Exists(defaultFile));
                        var defaultSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(defaultFile));
                        Assert.NotNull(defaultSettings);
                        Assert.Equal(desiredSettings.Voice, defaultSettings!.Voice);
                        Assert.Equal(desiredSettings.VoiceRate, defaultSettings.VoiceRate);
                        Assert.Equal(desiredSettings.Volume, defaultSettings.Volume);
                        Assert.Equal(desiredSettings.SaveConfigAsDefaultOnClose, defaultSettings.SaveConfigAsDefaultOnClose);
                        Assert.Equal(desiredSettings.UseDarkTheme, defaultSettings.UseDarkTheme);
                        Assert.Equal(desiredSettings.WindowLeft, defaultSettings.WindowLeft);
                        Assert.Equal(desiredSettings.WindowTop, defaultSettings.WindowTop);
                        Assert.Equal(desiredSettings.WindowWidth, defaultSettings.WindowWidth);
                        Assert.Equal(desiredSettings.WindowHeight, defaultSettings.WindowHeight);
                        Assert.Equal(desiredSettings.IsWindowMaximized, defaultSettings.IsWindowMaximized);
                        Assert.Equal(desiredSettings.Hotkey.Modifiers, defaultSettings.Hotkey!.Modifiers);
                        Assert.Equal(desiredSettings.Hotkey.Key, defaultSettings.Hotkey.Key);

                        var currentFile = Path.Combine(scope.DirectoryPath, "appsettings.json");
                        var storedSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(currentFile));
                        Assert.NotNull(storedSettings);
                        Assert.Equal(desiredSettings.Voice, storedSettings!.Voice);
                        Assert.Equal(desiredSettings.VoiceRate, storedSettings.VoiceRate);
                        Assert.Equal(desiredSettings.Volume, storedSettings.Volume);
                        Assert.Equal(desiredSettings.SaveConfigAsDefaultOnClose, storedSettings.SaveConfigAsDefaultOnClose);
                        Assert.Equal(desiredSettings.UseDarkTheme, storedSettings.UseDarkTheme);
                        Assert.Equal(desiredSettings.WindowLeft, storedSettings.WindowLeft);
                        Assert.Equal(desiredSettings.WindowTop, storedSettings.WindowTop);
                        Assert.Equal(desiredSettings.WindowWidth, storedSettings.WindowWidth);
                        Assert.Equal(desiredSettings.WindowHeight, storedSettings.WindowHeight);
                        Assert.Equal(desiredSettings.IsWindowMaximized, storedSettings.IsWindowMaximized);
                        Assert.Equal(desiredSettings.Hotkey.Modifiers, storedSettings.Hotkey!.Modifiers);
                        Assert.Equal(desiredSettings.Hotkey.Key, storedSettings.Hotkey.Key);

                        Assert.Empty(messageService.Errors);
                        Assert.Empty(messageService.Warnings);
                }

                [Fact]
                public void ResetToFactorySettings_RestoresDefaultValues()
                {
                        var messageService = new FakeMessageService();

                        using var scope = new TestDirectoryScope();
                        var service = CreateService(messageService);

                        var customSettings = new AppSettings
                        {
                                Voice = "Custom",
                                VoiceRate = 1.1,
                                Volume = 70,
                                SaveConfigAsDefaultOnClose = true,
                                UseDarkTheme = true,
                                WindowLeft = 400,
                                WindowTop = 500,
                                WindowWidth = 900,
                                WindowHeight = 600,
                                IsWindowMaximized = true,
                                Hotkey = new AppSettings.HotkeySettings
                                {
                                        Modifiers = "Ctrl",
                                        Key = "K",
                                }
                        };

                        service.SaveSettings(customSettings);

                        service.ResetToFactorySettings();

                        var current = service.GetCurrentSettings();
                        Assert.Equal("Microsoft David", current.Voice);
                        Assert.Equal(1.0, current.VoiceRate);
                        Assert.Equal(50, current.Volume);
                        Assert.False(current.SaveConfigAsDefaultOnClose);
                        Assert.False(current.UseDarkTheme);
                        Assert.Null(current.WindowLeft);
                        Assert.Null(current.WindowTop);
                        Assert.Null(current.WindowWidth);
                        Assert.Null(current.WindowHeight);
                        Assert.False(current.IsWindowMaximized);
                        Assert.Equal("Alt", current.Hotkey.Modifiers);
                        Assert.Equal("E", current.Hotkey.Key);

                        var settingsFile = Path.Combine(scope.DirectoryPath, "appsettings.json");
                        var storedSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(settingsFile));
                        Assert.NotNull(storedSettings);
                        Assert.Equal("Microsoft David", storedSettings!.Voice);
                        Assert.Equal(1.0, storedSettings.VoiceRate);
                        Assert.Equal(50, storedSettings.Volume);
                        Assert.False(storedSettings.SaveConfigAsDefaultOnClose);
                        Assert.False(storedSettings.UseDarkTheme);
                        Assert.Null(storedSettings.WindowLeft);
                        Assert.Null(storedSettings.WindowTop);
                        Assert.Null(storedSettings.WindowWidth);
                        Assert.Null(storedSettings.WindowHeight);
                        Assert.False(storedSettings.IsWindowMaximized);
                        Assert.Equal("Alt", storedSettings.Hotkey!.Modifiers);
                        Assert.Equal("E", storedSettings.Hotkey.Key);

                        Assert.Empty(messageService.Errors);
                }

                private static SettingsService CreateService(FakeMessageService messageService)
                {
                        return new SettingsService(NullLogger<SettingsService>.Instance, messageService);
                }
        }
}
