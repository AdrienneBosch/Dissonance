using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Speech.Synthesis;

using Dissonance;
using Dissonance.Services.DocumentReader;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;
using Dissonance.ViewModels;

using Xunit;

namespace Dissonance.Tests.ViewModels
{
        public class DocumentReaderViewModelTests
        {
                [Fact]
                public async Task LoadDocumentAsync_PopulatesPropertiesOnSuccess()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(), settingsService);

                        var success = await viewModel.LoadDocumentAsync(result.FilePath);

                        Assert.True(success);
                        var document = Assert.IsType<FlowDocument>(viewModel.Document);
                        var paragraphs = document.Blocks.OfType<Paragraph>().ToList();
                        Assert.Single(paragraphs);
                        Assert.Equal("Hello world", new TextRange(paragraphs[0].ContentStart, paragraphs[0].ContentEnd).Text.Trim());
                        Assert.Equal(result.PlainText, viewModel.PlainText);
                        Assert.Equal(result.FilePath, viewModel.FilePath);
                        Assert.Equal("sample.txt", viewModel.FileName);
                        Assert.True(viewModel.IsDocumentLoaded);
                        Assert.True(viewModel.CanReadDocument);
                        Assert.Equal(2, viewModel.WordCount);
                        Assert.Equal(result.PlainText.Length, viewModel.CharacterCount);
                        Assert.Null(viewModel.StatusMessage);
                        Assert.Null(viewModel.LastError);
                        Assert.True(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public async Task LoadDocumentAsync_WhenServiceThrows_ReturnsFalseAndSetsError()
                {
                        var service = new FailingDocumentReaderService(new InvalidOperationException("boom"));
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(), settingsService);

                        var success = await viewModel.LoadDocumentAsync("missing.txt");

                        Assert.False(success);
                        Assert.Null(viewModel.Document);
                        Assert.Null(viewModel.PlainText);
                        Assert.False(viewModel.IsDocumentLoaded);
                        Assert.False(viewModel.CanReadDocument);
                        Assert.Equal("boom", viewModel.StatusMessage);
                        Assert.IsType<InvalidOperationException>(viewModel.LastError);
                        Assert.False(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public void ClearDocumentCommand_ResetsState()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(), settingsService);

                        viewModel.ClearDocumentCommand.Execute(null);

                        Assert.Null(viewModel.Document);
                        Assert.Null(viewModel.PlainText);
                        Assert.Null(viewModel.FilePath);
                        Assert.False(viewModel.IsDocumentLoaded);
                        Assert.False(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public async Task CommandsReflectBusyStateWhileLoading()
                {
                        var tcs = new TaskCompletionSource<DocumentReadResult>();
                        var service = new PendingDocumentReaderService(tcs);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(), settingsService);

                        var loadTask = viewModel.LoadDocumentAsync("sample.txt");

                        Assert.False(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.False(viewModel.BrowseForDocumentCommand.CanExecute(null));

                        tcs.SetResult(new DocumentReadResult("sample.txt", string.Empty));
                        await loadTask;

                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public void ApplyPlaybackHotkeyCommand_SavesSettingsAndUpdatesState()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        settings.DocumentReaderHotkey.Key = string.Empty;
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(), settingsService);

                        viewModel.PlaybackHotkeyCombination = "MediaPlayPause";

                        Assert.True(viewModel.ApplyPlaybackHotkeyCommand.CanExecute(null));

                        viewModel.ApplyPlaybackHotkeyCommand.Execute(null);

                        Assert.Equal("MediaPlayPause", viewModel.PlaybackHotkeyCombination);
                        Assert.Equal(Key.MediaPlayPause, viewModel.PlaybackHotkeyKey);
                        Assert.Equal(ModifierKeys.None, viewModel.PlaybackHotkeyModifiers);
                        Assert.Equal("MediaPlayPause", settings.DocumentReaderHotkey.Key);
                        Assert.Equal(string.Empty, settings.DocumentReaderHotkey.Modifiers);
                        Assert.Equal(1, settingsService.SaveCalls);
                }

                [Fact]
                public async Task PlaybackHotkeyCommand_StopsPlaybackWhenToggleDisabled()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var ttsService = new StubTtsService(returnPrompt: true);
                        var viewModel = new DocumentReaderViewModel(service, ttsService, settingsService);

                        await viewModel.LoadDocumentAsync(result.FilePath);

                        viewModel.PlaybackHotkeyCombination = "MediaPlayPause";
                        viewModel.ApplyPlaybackHotkeyCommand.Execute(null);

                        viewModel.PlaybackHotkeyCommand.Execute(null);
                        Assert.True(viewModel.IsPlaying);

                        viewModel.PlaybackHotkeyCommand.Execute(null);

                        Assert.False(viewModel.IsPlaying);
                        Assert.False(viewModel.IsPaused);
                        Assert.Equal(1, ttsService.StopCalls);
                        Assert.Equal(1, settingsService.SaveCalls);
                        Assert.Equal(0, viewModel.CurrentCharacterIndex);
                }

                [Fact]
                public async Task PlaybackHotkeyCommand_TogglesPauseWhenEnabled()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var ttsService = new StubTtsService(returnPrompt: true);
                        var viewModel = new DocumentReaderViewModel(service, ttsService, settingsService);

                        await viewModel.LoadDocumentAsync(result.FilePath);

                        viewModel.PlaybackHotkeyTogglesPause = true;

                        viewModel.PlaybackHotkeyCommand.Execute(null);
                        Assert.True(viewModel.IsPlaying);

                        viewModel.PlaybackHotkeyCommand.Execute(null);
                        Assert.False(viewModel.IsPlaying);
                        Assert.True(viewModel.IsPaused);

                        viewModel.PlaybackHotkeyCommand.Execute(null);
                        Assert.True(viewModel.IsPlaying);
                        Assert.True(settings.DocumentReaderHotkey.UsePlayPauseToggle);
                        Assert.Equal(1, settingsService.SaveCalls);
                        Assert.True(ttsService.StopCalls >= 1);
                }

                [Fact]
                public async Task PlaybackHotkeyCommand_StartsPlaybackWhenDocumentPreviewUnavailable()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var ttsService = new StubTtsService(returnPrompt: true);
                        var viewModel = new DocumentReaderViewModel(service, ttsService, settingsService);

                        await viewModel.LoadDocumentAsync(result.FilePath);

                        var documentField = typeof(DocumentReaderViewModel).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic);
                        Assert.NotNull(documentField);
                        documentField!.SetValue(viewModel, null);

                        Assert.True(viewModel.PlaybackHotkeyCommand.CanExecute(null));

                        viewModel.PlaybackHotkeyCommand.Execute(null);

                        Assert.True(viewModel.IsPlaying);
                        Assert.Equal(result.PlainText.Length, viewModel.CharacterCount);
                }

                [Fact]
                public void RememberDocumentProgress_TogglePersistsSetting()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(), settingsService);

                        viewModel.RememberDocumentProgress = true;

                        Assert.True(viewModel.RememberDocumentProgress);
                        Assert.True(settings.RememberDocumentProgress);
                        Assert.Equal(1, settingsService.SaveCalls);

                        viewModel.RememberDocumentProgress = false;

                        Assert.False(viewModel.RememberDocumentProgress);
                        Assert.False(settings.RememberDocumentProgress);
                        Assert.Equal(2, settingsService.SaveCalls);
                }

                [Fact]
                public async Task RememberDocumentProgress_SavesPathAndIndex()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(returnPrompt: true), settingsService);

                        await viewModel.LoadDocumentAsync(result.FilePath);
                        viewModel.RememberDocumentProgress = true;

                        var property = typeof(DocumentReaderViewModel).GetProperty("CurrentCharacterIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        Assert.NotNull(property);
                        property!.SetValue(viewModel, 5);

                        Assert.Equal(result.FilePath, settings.DocumentReaderLastFilePath);
                        Assert.Equal(5, settings.DocumentReaderLastCharacterIndex);
                }

                [Fact]
                public async Task RememberDocumentProgress_ClearDocumentClearsStoredState()
                {
                        var result = new DocumentReadResult("sample.txt", "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var settings = CreateSettings();
                        var settingsService = new StubSettingsService(settings);
                        var viewModel = new DocumentReaderViewModel(service, new StubTtsService(returnPrompt: true), settingsService);

                        await viewModel.LoadDocumentAsync(result.FilePath);
                        viewModel.RememberDocumentProgress = true;

                        var property = typeof(DocumentReaderViewModel).GetProperty("CurrentCharacterIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        property!.SetValue(viewModel, result.PlainText.Length);

                        viewModel.ClearDocument();

                        Assert.Null(settings.DocumentReaderLastFilePath);
                        Assert.Null(settings.DocumentReaderLastCharacterIndex);
                }

                [Fact]
                public async Task RememberDocumentProgress_RestoresPositionWhenLoadingDocument()
                {
                        var tempFile = Path.GetTempFileName();
                        try
                        {
                                File.WriteAllText(tempFile, "Hello world");
                                var result = new DocumentReadResult(tempFile, "Hello world");
                                var service = new StubDocumentReaderService(result);
                                var settings = CreateSettings();
                                settings.RememberDocumentProgress = true;
                                settings.DocumentReaderLastFilePath = tempFile;
                                settings.DocumentReaderLastCharacterIndex = 4;
                                var settingsService = new StubSettingsService(settings);
                                var viewModel = new DocumentReaderViewModel(service, new StubTtsService(returnPrompt: true), settingsService);

                                await viewModel.LoadDocumentAsync(tempFile);

                                Assert.Equal(4, viewModel.CurrentCharacterIndex);
                        }
                        finally
                        {
                                if (File.Exists(tempFile))
                                        File.Delete(tempFile);
                        }
                }

                private static AppSettings CreateSettings()
                {
                        return new AppSettings
                        {
                                Hotkey = new AppSettings.HotkeySettings(),
                                DocumentReaderHotkey = new AppSettings.DocumentReaderHotkeySettings
                                {
                                        Key = "MediaPlayPause",
                                        Modifiers = string.Empty,
                                        UsePlayPauseToggle = false,
                                },
                                RememberDocumentProgress = false,
                                DocumentReaderLastFilePath = null,
                                DocumentReaderLastCharacterIndex = null,
                        };
                }

                private sealed class StubSettingsService : ISettingsService
                {
                        private AppSettings _settings;

                        public StubSettingsService(AppSettings settings)
                        {
                                _settings = settings;
                        }

                        public int SaveCalls { get; private set; }

                        public AppSettings GetCurrentSettings() => _settings;

                        public AppSettings LoadSettings() => _settings;

                        public void ResetToFactorySettings()
                        {
                        }

                        public void SaveSettings(AppSettings settings)
                        {
                                _settings = settings;
                        }

                        public bool SaveCurrentSettings()
                        {
                                SaveCalls++;
                                return true;
                        }

                        public bool SaveCurrentSettingsAsDefault()
                        {
                                return true;
                        }

                        public bool ExportSettings(string destinationPath) => true;

                        public bool ImportSettings(string sourcePath) => true;
                }

                private sealed class StubDocumentReaderService : IDocumentReaderService
                {
                        private readonly DocumentReadResult _result;

                        public StubDocumentReaderService(DocumentReadResult result)
                        {
                                _result = result;
                        }

                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return Task.FromResult(_result);
                        }
                }

                private sealed class FailingDocumentReaderService : IDocumentReaderService
                {
                        private readonly Exception _exception;

                        public FailingDocumentReaderService(Exception exception)
                        {
                                _exception = exception;
                        }

                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return Task.FromException<DocumentReadResult>(_exception);
                        }
                }

                private sealed class PendingDocumentReaderService : IDocumentReaderService
                {
                        private readonly TaskCompletionSource<DocumentReadResult> _completion;

                        public PendingDocumentReaderService(TaskCompletionSource<DocumentReadResult> completion)
                        {
                                _completion = completion;
                        }

                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return _completion.Task;
                        }
                }

                private sealed class StubTtsService : ITTSService
                {
                        private readonly bool _returnPrompt;

                        public StubTtsService(bool returnPrompt = false)
                        {
                                _returnPrompt = returnPrompt;
                        }

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

                        public int StopCalls { get; private set; }

                        public Prompt? LastPrompt { get; private set; }

                        public void SetTTSParameters(string voice, double rate, int volume)
                        {
                        }

                        public Prompt? Speak(string text)
                        {
                                if (!_returnPrompt)
                                        return null;

                                LastPrompt = new Prompt(text);
                                return LastPrompt;
                        }

                        public void Stop()
                        {
                                StopCalls++;
                        }
                }
        }
}
