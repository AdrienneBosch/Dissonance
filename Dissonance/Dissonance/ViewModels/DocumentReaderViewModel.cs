using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Speech.Synthesis;

using Dissonance.Infrastructure.Commands;
using Dissonance.Services.DocumentReader;
using Dissonance.Services.TTSService;

using Microsoft.Win32;

namespace Dissonance.ViewModels
{
        public class DocumentReaderViewModel : INotifyPropertyChanged
        {
                private readonly IDocumentReaderService _documentReaderService;
                private readonly RelayCommandNoParam _clearDocumentCommand;
                private readonly RelayCommandNoParam _browseForDocumentCommand;
                private readonly RelayCommandNoParam _playPauseCommand;
                private readonly RelayCommandNoParam _seekBackwardCommand;
                private readonly RelayCommandNoParam _seekForwardCommand;
                private readonly ITTSService _ttsService;
                private FlowDocument? _document;
                private string? _plainText;
                private string? _filePath;
                private string? _statusMessage;
                private bool _isBusy;
                private Exception? _lastError;
                private Prompt? _currentPrompt;
                private int _currentCharacterIndex;
                private int _playbackStartCharacterIndex;
                private TimeSpan _currentAudioPosition;
                private TimeSpan _playbackStartAudioPosition;
                private bool _isPlaying;
                private bool _isPaused;
                private bool _isStoppingForPause;
                private bool _isStoppingForSeek;
                private int? _pendingSeekCharacterIndex;
                private TimeSpan _pendingSeekAudioPosition;
                private double _charactersPerSecond;
                private readonly List<(TimeSpan Time, int CharacterIndex)> _progressHistory = new();

                private const double DefaultCharactersPerSecond = 15d;

                public DocumentReaderViewModel(IDocumentReaderService documentReaderService, ITTSService ttsService)
                {
                        _documentReaderService = documentReaderService ?? throw new ArgumentNullException(nameof(documentReaderService));
                        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
                        _clearDocumentCommand = new RelayCommandNoParam(ClearDocument, () => !IsBusy && (IsDocumentLoaded || HasStatusMessage));
                        _browseForDocumentCommand = new RelayCommandNoParam(BrowseForDocument, () => !IsBusy);
                        _playPauseCommand = new RelayCommandNoParam(TogglePlayback, () => !IsBusy && CanReadDocument);
                        _seekBackwardCommand = new RelayCommandNoParam(() => SeekBy(TimeSpan.FromSeconds(-10)), () => !IsBusy && CanReadDocument);
                        _seekForwardCommand = new RelayCommandNoParam(() => SeekBy(TimeSpan.FromSeconds(10)), () => !IsBusy && CanReadDocument);

                        _ttsService.SpeechCompleted += OnSpeechCompleted;
                        _ttsService.SpeechProgress += OnSpeechProgress;
                }

                public event PropertyChangedEventHandler? PropertyChanged;

                public FlowDocument? Document
                {
                        get => _document;
                        private set
                        {
                                if (ReferenceEquals(_document, value))
                                        return;

                                _document = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(IsDocumentLoaded));
                                OnPropertyChanged(nameof(CanReadDocument));
                                OnPropertyChanged(nameof(FileName));
                                UpdateCommandStates();
                        }
                }

                public string? PlainText
                {
                        get => _plainText;
                        private set
                        {
                                if (_plainText == value)
                                        return;

                                _plainText = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(HasPlainText));
                                OnPropertyChanged(nameof(WordCount));
                                OnPropertyChanged(nameof(CharacterCount));
                                OnPropertyChanged(nameof(CanReadDocument));
                        }
                }

                public string? FilePath
                {
                        get => _filePath;
                        set
                        {
                                if (_filePath == value)
                                        return;

                                _filePath = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(FileName));
                        }
                }

                public string? FileName => string.IsNullOrWhiteSpace(FilePath) ? null : Path.GetFileName(FilePath);

                public bool IsDocumentLoaded => Document != null;

                public bool HasPlainText => !string.IsNullOrWhiteSpace(PlainText);

                public bool CanReadDocument => IsDocumentLoaded && HasPlainText;

                public bool IsPlaying
                {
                        get => _isPlaying;
                        private set
                        {
                                if (_isPlaying == value)
                                        return;

                                _isPlaying = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(PlayPauseLabel));
                        }
                }

                public bool IsPaused
                {
                        get => _isPaused;
                        private set
                        {
                                if (_isPaused == value)
                                        return;

                                _isPaused = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(PlayPauseLabel));
                        }
                }

                public TimeSpan CurrentAudioPosition
                {
                        get => _currentAudioPosition;
                        private set
                        {
                                if (_currentAudioPosition == value)
                                        return;

                                _currentAudioPosition = value;
                                OnPropertyChanged();
                        }
                }

                public int CurrentCharacterIndex
                {
                        get => _currentCharacterIndex;
                        private set
                        {
                                if (_currentCharacterIndex == value)
                                        return;

                                _currentCharacterIndex = value;
                                OnPropertyChanged();
                        }
                }

                public string PlayPauseLabel => IsPlaying && !IsPaused ? "Pause" : "Play";

                public int WordCount => _plainText?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

                public int CharacterCount => _plainText?.Length ?? 0;

                public bool IsBusy
                {
                        get => _isBusy;
                        private set
                        {
                                if (_isBusy == value)
                                        return;

                                _isBusy = value;
                                OnPropertyChanged();
                                UpdateCommandStates();
                        }
                }

                public string? StatusMessage
                {
                        get => _statusMessage;
                        private set
                        {
                                if (_statusMessage == value)
                                        return;

                                _statusMessage = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(HasStatusMessage));
                                UpdateCommandStates();
                        }
                }

                public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

                public Exception? LastError
                {
                        get => _lastError;
                        private set
                        {
                                if (ReferenceEquals(_lastError, value))
                                        return;

                                _lastError = value;
                                OnPropertyChanged();
                        }
                }

                public ICommand ClearDocumentCommand => _clearDocumentCommand;

                public ICommand BrowseForDocumentCommand => _browseForDocumentCommand;

                public ICommand PlayPauseCommand => _playPauseCommand;

                public ICommand SeekBackwardCommand => _seekBackwardCommand;

                public ICommand SeekForwardCommand => _seekForwardCommand;

                public async Task<bool> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                {
                        if (string.IsNullOrWhiteSpace(filePath))
                                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

                        try
                        {
                                IsBusy = true;
                                LastError = null;
                                StatusMessage = null;

                                var result = await _documentReaderService.ReadDocumentAsync(filePath, cancellationToken);

                                ApplyResult(result);
                                StatusMessage = null;
                                return true;
                        }
                        catch (OperationCanceledException)
                        {
                                ClearDocument();
                                StatusMessage = "Document loading was canceled.";
                                throw;
                        }
                        catch (Exception ex)
                        {
                                ClearDocument();
                                LastError = ex;
                                StatusMessage = ex.Message;
                                return false;
                        }
                        finally
                        {
                                IsBusy = false;
                        }
                }

                public void ClearDocument()
                {
                        Document = null;
                        PlainText = null;
                        FilePath = null;
                        LastError = null;
                        StatusMessage = null;
                        ResetPlaybackState();
                }

                private async void BrowseForDocument()
                {
                        var dialog = new OpenFileDialog
                        {
                                Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*",
                                CheckFileExists = true,
                                CheckPathExists = true,
                                Title = "Open document",
                                Multiselect = false
                        };

                        if (!string.IsNullOrWhiteSpace(FilePath))
                        {
                                var directory = Path.GetDirectoryName(FilePath);
                                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                                        dialog.InitialDirectory = directory;
                        }

                        var result = dialog.ShowDialog();
                        if (result != true)
                                return;

                        try
                        {
                                await LoadDocumentAsync(dialog.FileName);
                        }
                        catch (OperationCanceledException)
                        {
                                // Cancellation has already updated the status message; swallow to avoid surfacing to the UI.
                        }
                }

                private void ApplyResult(DocumentReadResult result)
                {
                        if (result == null)
                                throw new ArgumentNullException(nameof(result));

                        Document = CreateFlowDocument(result.PlainText);
                        PlainText = result.PlainText;
                        FilePath = result.FilePath;
                        LastError = null;
                        ResetPlaybackState();
                }

                private void UpdateCommandStates()
                {
                        _clearDocumentCommand.RaiseCanExecuteChanged();
                        _browseForDocumentCommand.RaiseCanExecuteChanged();
                        _playPauseCommand.RaiseCanExecuteChanged();
                        _seekBackwardCommand.RaiseCanExecuteChanged();
                        _seekForwardCommand.RaiseCanExecuteChanged();
                }

                private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }

                private static FlowDocument CreateFlowDocument(string content)
                {
                        var document = new FlowDocument();
                        if (string.IsNullOrEmpty(content))
                        {
                                document.Blocks.Add(new Paragraph(new Run(string.Empty)));
                                return document;
                        }

                        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
                        var lines = normalized.Split('\n');
                        foreach (var line in lines)
                        {
                                document.Blocks.Add(new Paragraph(new Run(line)));
                        }

                        return document;
                }

                private void TogglePlayback()
                {
                        if (!CanReadDocument)
                                return;

                        if (!IsPlaying && !IsPaused)
                        {
                                StartPlaybackFromCurrentPosition();
                                return;
                        }

                        if (IsPlaying && !IsPaused)
                        {
                                PausePlayback();
                                return;
                        }

                        if (IsPaused)
                        {
                                StartPlaybackFromCurrentPosition();
                        }
                }

                private void PausePlayback()
                {
                        if (_currentPrompt == null)
                        {
                                IsPlaying = false;
                                IsPaused = true;
                                return;
                        }

                        _isStoppingForPause = true;
                        IsPlaying = false;
                        IsPaused = true;
                        _ttsService.Stop();
                }

                private void StartPlaybackFromCurrentPosition()
                {
                        if (PlainText == null)
                                return;

                        if (CurrentCharacterIndex >= PlainText.Length)
                        {
                                CurrentCharacterIndex = 0;
                                CurrentAudioPosition = TimeSpan.Zero;
                        }

                        var remainingLength = PlainText.Length - CurrentCharacterIndex;
                        if (remainingLength <= 0)
                        {
                                IsPlaying = false;
                                IsPaused = false;
                                return;
                        }

                        var textToSpeak = PlainText.Substring(CurrentCharacterIndex);
                        _playbackStartCharacterIndex = CurrentCharacterIndex;
                        _playbackStartAudioPosition = EstimateTimeFromCharacterIndex(CurrentCharacterIndex);
                        _currentPrompt = _ttsService.Speak(textToSpeak);
                        _pendingSeekCharacterIndex = null;
                        _pendingSeekAudioPosition = TimeSpan.Zero;

                        if (_currentPrompt != null)
                        {
                                IsPlaying = true;
                                IsPaused = false;
                                _isStoppingForPause = false;
                                _isStoppingForSeek = false;
                        }
                        else
                        {
                                IsPlaying = false;
                                IsPaused = false;
                        }
                }

                private void SeekBy(TimeSpan offset)
                {
                        if (!CanReadDocument || PlainText == null)
                                return;

                        var seconds = offset.TotalSeconds;
                        if (Math.Abs(seconds) < double.Epsilon)
                                return;

                        var cps = _charactersPerSecond > 0 ? _charactersPerSecond : DefaultCharactersPerSecond;
                        if (cps <= 0)
                                cps = DefaultCharactersPerSecond;

                        var deltaChars = (int)Math.Round(cps * seconds);
                        if (deltaChars == 0)
                                deltaChars = seconds > 0 ? 1 : -1;

                        var targetIndex = Math.Clamp(CurrentCharacterIndex + deltaChars, 0, PlainText.Length);
                        var targetTime = EstimateTimeFromCharacterIndex(targetIndex);

                        CurrentCharacterIndex = targetIndex;
                        CurrentAudioPosition = targetTime;

                        if (IsPlaying)
                        {
                                _pendingSeekCharacterIndex = targetIndex;
                                _pendingSeekAudioPosition = targetTime;
                                _isStoppingForSeek = true;
                                IsPlaying = false;
                                _ttsService.Stop();
                        }
                }

                private void ResetPlaybackState()
                {
                        if (_currentPrompt != null)
                                _ttsService.Stop();

                        _currentPrompt = null;
                        _playbackStartCharacterIndex = 0;
                        _playbackStartAudioPosition = TimeSpan.Zero;
                        _charactersPerSecond = 0;
                        _progressHistory.Clear();
                        _isStoppingForPause = false;
                        _isStoppingForSeek = false;
                        _pendingSeekCharacterIndex = null;
                        _pendingSeekAudioPosition = TimeSpan.Zero;
                        CurrentCharacterIndex = 0;
                        CurrentAudioPosition = TimeSpan.Zero;
                        IsPlaying = false;
                        IsPaused = false;
                }

                private TimeSpan EstimateTimeFromCharacterIndex(int characterIndex)
                {
                        if (_progressHistory.Count > 0)
                        {
                                for (var i = _progressHistory.Count - 1; i >= 0; i--)
                                {
                                        var entry = _progressHistory[i];
                                        if (entry.CharacterIndex <= characterIndex)
                                        {
                                                var cps = _charactersPerSecond > 0 ? _charactersPerSecond : DefaultCharactersPerSecond;
                                                var delta = characterIndex - entry.CharacterIndex;
                                                var additional = cps > 0 ? TimeSpan.FromSeconds(delta / cps) : TimeSpan.Zero;
                                                return entry.Time + additional;
                                        }
                                }
                        }

                        if (_charactersPerSecond > 0)
                                return TimeSpan.FromSeconds(characterIndex / _charactersPerSecond);

                        return TimeSpan.FromSeconds(characterIndex / DefaultCharactersPerSecond);
                }

                private void OnSpeechProgress(object? sender, SpeakProgressEventArgs e)
                {
                        if (PlainText == null)
                                return;

                        var updatedIndex = _playbackStartCharacterIndex + e.CharacterPosition;
                        if (updatedIndex > PlainText.Length)
                                updatedIndex = PlainText.Length;

                        if (updatedIndex > CurrentCharacterIndex)
                                CurrentCharacterIndex = updatedIndex;

                        var updatedTime = _playbackStartAudioPosition + e.AudioPosition;
                        if (updatedTime > CurrentAudioPosition)
                                CurrentAudioPosition = updatedTime;

                        if (updatedTime.TotalSeconds > 0)
                                _charactersPerSecond = Math.Max(_charactersPerSecond, CurrentCharacterIndex / updatedTime.TotalSeconds);

                        if (_progressHistory.Count == 0 || _progressHistory[^1].CharacterIndex != CurrentCharacterIndex)
                                _progressHistory.Add((CurrentAudioPosition, CurrentCharacterIndex));
                }

                private void OnSpeechCompleted(object? sender, SpeakCompletedEventArgs e)
                {
                        if (_currentPrompt != null && !ReferenceEquals(e.Prompt, _currentPrompt))
                                return;

                        _currentPrompt = null;

                        if (_isStoppingForPause)
                        {
                                _isStoppingForPause = false;
                                IsPlaying = false;
                                IsPaused = true;
                                return;
                        }

                        if (_isStoppingForSeek)
                        {
                                _isStoppingForSeek = false;
                                if (_pendingSeekCharacterIndex.HasValue)
                                {
                                        CurrentCharacterIndex = _pendingSeekCharacterIndex.Value;
                                        CurrentAudioPosition = _pendingSeekAudioPosition;
                                        _pendingSeekCharacterIndex = null;
                                        StartPlaybackFromCurrentPosition();
                                }
                                return;
                        }

                        if (e.Cancelled)
                        {
                                IsPlaying = false;
                                return;
                        }

                        IsPlaying = false;
                        IsPaused = false;

                        if (PlainText != null)
                        {
                                CurrentCharacterIndex = PlainText.Length;
                                CurrentAudioPosition = EstimateTimeFromCharacterIndex(CurrentCharacterIndex);
                        }
                }
        }
}
