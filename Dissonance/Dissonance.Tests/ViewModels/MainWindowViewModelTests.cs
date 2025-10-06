using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using Dissonance;
using Dissonance.Managers;
using Dissonance.Services.ClipboardService;
using Dissonance.Services.DocumentReader;
using Dissonance.Services.HotkeyService;
using Dissonance.Services.MessageService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.ThemeService;
using Dissonance.Services.StatusAnnouncements;
using Dissonance.Services.TTSService;
using Dissonance.Tests.TestInfrastructure;
using Dissonance.ViewModels;

using Microsoft.Extensions.Logging;

using Xunit;

namespace Dissonance.Tests.ViewModels
{
        public class MainWindowViewModelTests
        {
                [WindowsFact]
                public void Constructor_InitializesFromSettings()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: true);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        var viewModel = testEnvironment.ViewModel;

                        Assert.True(viewModel.IsDarkTheme);
                        Assert.Equal("Dark Mode", viewModel.CurrentThemeName);
                        Assert.Equal(testEnvironment.SettingsService.Current.SaveConfigAsDefaultOnClose, viewModel.SaveConfigAsDefaultOnClose);
                        Assert.Equal("Alt+E", viewModel.HotkeyCombination);
                        Assert.Equal(testEnvironment.SettingsService.Current.Voice, viewModel.Voice);
                        Assert.Equal(testEnvironment.SettingsService.Current.VoiceRate, viewModel.VoiceRate);
                        Assert.Equal(testEnvironment.SettingsService.Current.Volume, viewModel.Volume);
                        Assert.Contains(testEnvironment.SettingsService.Current.Voice, viewModel.AvailableVoices);

                        Assert.Equal(AppTheme.Dark, testEnvironment.ThemeService.CurrentTheme);
                        Assert.Equal(testEnvironment.SettingsService.Current.Voice, testEnvironment.TtsService.LastVoice);
                        Assert.Equal(testEnvironment.SettingsService.Current.VoiceRate, testEnvironment.TtsService.LastRate);
                        Assert.Equal(testEnvironment.SettingsService.Current.Volume, testEnvironment.TtsService.LastVolume);
                }

                [WindowsFact]
                public void IsDarkTheme_TogglesThemeAndSavesSetting()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        var viewModel = testEnvironment.ViewModel;

                        viewModel.IsDarkTheme = true;

                        Assert.True(viewModel.IsDarkTheme);
                        Assert.Equal(AppTheme.Dark, testEnvironment.ThemeService.CurrentTheme);
                        Assert.True(testEnvironment.SettingsService.Current.UseDarkTheme);
                        Assert.Equal(1, testEnvironment.SettingsService.SaveCurrentSettingsCalls);
                }

                [WindowsFact]
                public void Volume_UpdatesSettingsAndTTS()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        var viewModel = testEnvironment.ViewModel;

                        viewModel.Volume = 75;

                        Assert.Equal(75, testEnvironment.SettingsService.Current.Volume);
                        Assert.Equal(75, testEnvironment.TtsService.LastVolume);
                }

                [WindowsFact]
                public void Volume_ThrowsWhenOutOfRange()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        var viewModel = testEnvironment.ViewModel;

                        Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.Volume = 200);
                }

                [WindowsFact]
                public void Voice_SetterRejectsUnknownVoice()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        var viewModel = testEnvironment.ViewModel;

                        Assert.Throws<ArgumentException>(() => viewModel.Voice = "Unknown Voice");
                }

                [WindowsFact]
                public void ApplyHotkeyCommand_RegistersHotkeyAndSaves()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        var viewModel = testEnvironment.ViewModel;

                        viewModel.HotkeyCombination = "Ctrl+F";
                        Assert.True(viewModel.ApplyHotkeyCommand.CanExecute(null));

                        viewModel.ApplyHotkeyCommand.Execute(null);

                        Assert.Equal("Ctrl+F", viewModel.HotkeyCombination);
                        Assert.Equal("Ctrl", testEnvironment.SettingsService.Current.Hotkey.Modifiers);
                        Assert.Equal("F", testEnvironment.SettingsService.Current.Hotkey.Key);
                        Assert.Equal("Ctrl+F", testEnvironment.HotkeyService.LastRegisteredHotkey);
                        Assert.Equal(1, testEnvironment.SettingsService.SaveCurrentSettingsCalls);
                }

                [WindowsFact]
                public void OnWindowClosing_SavesDefaultsWhenRequested()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }
                        testEnvironment.SettingsService.Current.SaveConfigAsDefaultOnClose = true;

                        testEnvironment.ViewModel.OnWindowClosing();

                        Assert.Equal(1, testEnvironment.SettingsService.SaveCurrentSettingsAsDefaultCalls);
                }

                [WindowsFact]
                public void NavigateToSectionCommand_SetsAndClearsSelection()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }

                        var viewModel = testEnvironment.ViewModel;
                        Assert.True(viewModel.IsHomeSelected);
                        Assert.Null(viewModel.SelectedSection);

                        var targetSection = viewModel.NavigationSections.FirstOrDefault();
                        Assert.NotNull(targetSection);

                        viewModel.NavigateToSectionCommand.Execute(targetSection);

                        Assert.Same(targetSection, viewModel.SelectedSection);
                        Assert.False(viewModel.IsHomeSelected);

                        viewModel.NavigateToSectionCommand.Execute(null);

                        Assert.Null(viewModel.SelectedSection);
                        Assert.True(viewModel.IsHomeSelected);
                }

                [WindowsFact]
                public void PendingNavigationSection_DoesNotChangeSelectedSection()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }

                        var viewModel = testEnvironment.ViewModel;
                        var targetSection = viewModel.NavigationSections.FirstOrDefault();
                        Assert.NotNull(targetSection);

                        viewModel.PendingNavigationSection = targetSection;

                        Assert.Null(viewModel.SelectedSection);
                        Assert.Same(targetSection, viewModel.PendingNavigationSection);
                }

                [WindowsFact]
                public void SelectedSection_UpdatesPendingNavigationSection()
                {
                        var testEnvironment = CreateTestEnvironment(useDarkTheme: false);
                        if (testEnvironment is null)
                        {
                                return;
                        }

                        var viewModel = testEnvironment.ViewModel;
                        var targetSection = viewModel.NavigationSections.FirstOrDefault();
                        Assert.NotNull(targetSection);

                        viewModel.NavigateToSectionCommand.Execute(targetSection);

                        Assert.Same(targetSection, viewModel.PendingNavigationSection);

                        viewModel.NavigateToSectionCommand.Execute(null);

                        Assert.Null(viewModel.PendingNavigationSection);
                }

                private static TestEnvironment? CreateTestEnvironment(bool useDarkTheme)
                {
                        var synthesizer = new SpeechSynthesizer();
                        var voices = synthesizer.GetInstalledVoices();
                        if (voices.Count == 0)
                        {
                                return null;
                        }

                        var voiceName = voices[0].VoiceInfo.Name;

                        var settings = new AppSettings
                        {
                                Voice = voiceName,
                                VoiceRate = 1.0,
                                Volume = 50,
                                SaveConfigAsDefaultOnClose = false,
                                UseDarkTheme = useDarkTheme,
                                Hotkey = new AppSettings.HotkeySettings
                                {
                                        Modifiers = "Alt",
                                        Key = "E"
                                },
                                DocumentReaderHotkey = new AppSettings.DocumentReaderHotkeySettings
                                {
                                        Key = "MediaPlayPause",
                                        Modifiers = string.Empty,
                                        UsePlayPauseToggle = false,
                                }
                        };

                        var settingsService = new TestSettingsService(settings);
                        var ttsService = new TestTtsService();
                        var hotkeyService = new TestHotkeyService();
                        var themeService = new TestThemeService();
                        var messageService = new FakeMessageService();
                        var clipboardService = new TestClipboardService();
                        var statusService = new TestStatusAnnouncementService();
                        var documentReaderService = new TestDocumentReaderService();
                        var documentReaderViewModel = new DocumentReaderViewModel(documentReaderService, ttsService, settingsService, hotkeyService);
                        var clipboardManager = new ClipboardManager(clipboardService, new TestLogger<ClipboardManager>(), statusService);

                        var viewModel = new MainWindowViewModel(settingsService, ttsService, hotkeyService, themeService, messageService, clipboardManager, statusService, documentReaderViewModel);

                        return new TestEnvironment(viewModel, settingsService, ttsService, hotkeyService, themeService, clipboardManager, statusService, documentReaderViewModel);
                }

                private sealed record TestEnvironment(MainWindowViewModel ViewModel, TestSettingsService SettingsService, TestTtsService TtsService, TestHotkeyService HotkeyService, TestThemeService ThemeService, ClipboardManager ClipboardManager, TestStatusAnnouncementService StatusAnnouncementService, DocumentReaderViewModel DocumentReaderViewModel);

                private sealed class TestDocumentReaderService : IDocumentReaderService
                {
                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return Task.FromResult(new DocumentReadResult(filePath, "Sample"));
                        }
                }

                private sealed class TestStatusAnnouncementService : IStatusAnnouncementService
                {
                        private readonly List<StatusAnnouncement> _history = new List<StatusAnnouncement>();

                        public StatusAnnouncement? Latest { get; private set; }

                        public IReadOnlyList<StatusAnnouncement> History => _history.AsReadOnly();

                        public event EventHandler<StatusAnnouncement>? StatusAnnounced;

                        public void Announce(StatusAnnouncement announcement)
                        {
                                if (announcement == null)
                                        throw new ArgumentNullException(nameof(announcement));

                                _history.Add(announcement);
                                Latest = announcement;
                                StatusAnnounced?.Invoke(this, announcement);
                        }

                        public void Announce(string message, StatusSeverity severity = StatusSeverity.Info)
                        {
                                Announce(new StatusAnnouncement(message ?? string.Empty, severity));
                        }

                        public void AnnounceFromResource(string resourceKey, string fallbackMessage, StatusSeverity severity = StatusSeverity.Info)
                        {
                                Announce(fallbackMessage, severity);
                        }
                }

                private sealed class TestSettingsService : ISettingsService
                {
                        private AppSettings _current;

                        public TestSettingsService(AppSettings current)
                        {
                                _current = Clone(current);
                        }

                        public AppSettings Current => _current;

                        public int SaveCurrentSettingsCalls { get; private set; }

                        public int SaveCurrentSettingsAsDefaultCalls { get; private set; }

                        public AppSettings GetCurrentSettings() => _current;

                        public AppSettings LoadSettings() => _current;

                        public void ResetToFactorySettings()
                        {
                        }

                        public void SaveSettings(AppSettings settings)
                        {
                                _current = Clone(settings);
                        }

                        public bool SaveCurrentSettings()
                        {
                                SaveCurrentSettingsCalls++;
                                return true;
                        }

                        public bool SaveCurrentSettingsAsDefault()
                        {
                                SaveCurrentSettingsAsDefaultCalls++;
                                return true;
                        }

                        public bool ExportSettings(string destinationPath) => true;

                        public bool ImportSettings(string sourcePath) => true;

                        private static AppSettings Clone(AppSettings settings)
                        {
                                return new AppSettings
                                {
                                        Voice = settings.Voice,
                                        VoiceRate = settings.VoiceRate,
                                        Volume = settings.Volume,
                                        SaveConfigAsDefaultOnClose = settings.SaveConfigAsDefaultOnClose,
                                        UseDarkTheme = settings.UseDarkTheme,
                                        Hotkey = new AppSettings.HotkeySettings
                                        {
                                                Modifiers = settings.Hotkey?.Modifiers ?? string.Empty,
                                                Key = settings.Hotkey?.Key ?? string.Empty
                                        },
                                        DocumentReaderHotkey = new AppSettings.DocumentReaderHotkeySettings
                                        {
                                                Modifiers = settings.DocumentReaderHotkey?.Modifiers ?? string.Empty,
                                                Key = settings.DocumentReaderHotkey?.Key ?? string.Empty,
                                                UsePlayPauseToggle = settings.DocumentReaderHotkey?.UsePlayPauseToggle ?? false,
                                        }
                                };
                        }
                }

                private sealed class TestTtsService : ITTSService
                {
                        public string? LastVoice { get; private set; }

                        public double LastRate { get; private set; }

                        public int LastVolume { get; private set; }

                        public Prompt? LastPrompt { get; private set; }

                        public event EventHandler<SpeakCompletedEventArgs>? SpeechCompleted
                        {
                                add { }
                                remove { }
                        }

                        public event EventHandler<SpeakProgressEventArgs>? SpeechProgress
                        {
                                add { }
                                remove { }
                        }

                        public void SetTTSParameters(string voice, double rate, int volume)
                        {
                                LastVoice = voice;
                                LastRate = rate;
                                LastVolume = volume;
                        }

                        public Prompt? Speak(string text)
                        {
                                LastPrompt = new Prompt(text);
                                return LastPrompt;
                        }

                        public void Stop()
                        {
                        }
                }

                private sealed class TestHotkeyService : IHotkeyService
                {
                        public string? LastRegisteredHotkey { get; private set; }
                        public List<string> AdditionalRegistrations { get; } = new();

                        public event Action? HotkeyPressed
                        {
                                add { }
                                remove { }
                        }

                        public void Dispose()
                        {
                        }

                        public void Initialize(System.Windows.Window mainWindow)
                        {
                        }

                        public void RegisterHotkey(AppSettings.HotkeySettings hotkey)
                        {
                                LastRegisteredHotkey = $"{hotkey.Modifiers}+{hotkey.Key}";
                        }

                        public IDisposable? RegisterHotkey(AppSettings.HotkeySettings hotkey, Action callback, bool allowEmptyModifiers = false)
                        {
                                AdditionalRegistrations.Add($"{hotkey.Modifiers}|{hotkey.Key}|allowEmpty:{allowEmptyModifiers}");
                                return new StubRegistration();
                        }

                        public void UnregisterHotkey()
                        {
                                LastRegisteredHotkey = null;
                        }

                        private sealed class StubRegistration : IDisposable
                        {
                                public void Dispose()
                                {
                                }
                        }
                }

                private sealed class TestThemeService : IThemeService
                {
                        public IReadOnlyCollection<AppTheme> AvailableThemes { get; } = new[] { AppTheme.Light, AppTheme.Dark };

                        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

                        public void ApplyTheme(AppTheme theme)
                        {
                                CurrentTheme = theme;
                        }
                }

                private sealed class TestClipboardService : IClipboardService
                {
                        private string? _text;

                        public string? GetClipboardText() => _text;

                        public bool IsTextAvailable() => !string.IsNullOrEmpty(_text);

                        public void SetClipboardText(string? text)
                        {
                                _text = text;
                        }
                }

                private sealed class TestLogger<T> : ILogger<T>
                {
                        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

                        public bool IsEnabled(LogLevel logLevel) => false;

                        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                        {
                        }

                        private sealed class NullScope : IDisposable
                        {
                                public static readonly NullScope Instance = new NullScope();

                                public void Dispose()
                                {
                                }
                        }
                }
        }
}
