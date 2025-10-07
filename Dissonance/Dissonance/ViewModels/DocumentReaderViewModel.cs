using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Speech.Synthesis;
using System.Security.Cryptography;
using System.Text;

using Dissonance.Infrastructure.Commands;
using Dissonance.Services.DocumentReader;
using Dissonance.Services.SettingsService;
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
                private readonly RelayCommandNoParam _applyPlaybackHotkeyCommand;
                private readonly RelayCommandNoParam _playbackHotkeyCommand;
                private readonly ITTSService _ttsService;
                private readonly ISettingsService _settingsService;
                private readonly ObservableCollection<DocumentSection> _sections = new();
                private readonly ReadOnlyObservableCollection<DocumentSection> _readOnlySections;
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
                private string _playbackHotkeyCombination = string.Empty;
                private string _lastAppliedPlaybackHotkeyCombination = string.Empty;
                private Key _playbackHotkeyKey = Key.None;
                private ModifierKeys _playbackHotkeyModifiers = ModifierKeys.None;
                private bool _playbackHotkeyTogglesPause;
                private readonly IReadOnlyList<HighlightColorOption> _highlightColorOptions;
                private HighlightColorOption _selectedHighlightColor = null!;
                private Brush _highlightBrush = Brushes.Transparent;
                private int _highlightStartIndex;
                private int _highlightLength;
                private bool _rememberDocumentProgress;
                private DocumentSection? _selectedSection;
                private bool _suppressSectionNavigation;
                private DateTime _lastProgressSaveTime = DateTime.MinValue;
                private int _lastPersistedCharacterIndex;
                private bool _suspendProgressPersistence;
                private bool _documentMetadataDirty = true;
                private DocumentMetadata? _currentDocumentMetadata;
                private DocumentMetadata? _lastPersistedMetadata;

                private static readonly TimeSpan ProgressPersistInterval = TimeSpan.FromSeconds(2);

                private const double DefaultCharactersPerSecond = 15d;
                private const string ThemeAccentHighlightId = "ThemeAccent";

                public DocumentReaderViewModel(IDocumentReaderService documentReaderService, ITTSService ttsService, ISettingsService settingsService)
                {
                        _documentReaderService = documentReaderService ?? throw new ArgumentNullException(nameof(documentReaderService));
                        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
                        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
                        _readOnlySections = new ReadOnlyObservableCollection<DocumentSection>(_sections);
                        _clearDocumentCommand = new RelayCommandNoParam(ClearDocument, () => !IsBusy && (IsDocumentLoaded || HasStatusMessage));
                        _browseForDocumentCommand = new RelayCommandNoParam(BrowseForDocument, () => !IsBusy);
                        _playPauseCommand = new RelayCommandNoParam(TogglePlayback, () => !IsBusy && CanReadDocument);
                        _seekBackwardCommand = new RelayCommandNoParam(() => SeekBy(TimeSpan.FromSeconds(-10)), () => !IsBusy && CanReadDocument);
                        _seekForwardCommand = new RelayCommandNoParam(() => SeekBy(TimeSpan.FromSeconds(10)), () => !IsBusy && CanReadDocument);
                        _playbackHotkeyCommand = new RelayCommandNoParam(ExecutePlaybackHotkey, () => !IsBusy && (HasPlainText || IsPlaying || IsPaused));
                        _applyPlaybackHotkeyCommand = new RelayCommandNoParam(ApplyPlaybackHotkey, CanApplyPlaybackHotkey);

                        _ttsService.SpeechCompleted += OnSpeechCompleted;
                        _ttsService.SpeechProgress += OnSpeechProgress;

                        InitializePlaybackHotkeyFromSettings();
                        _highlightColorOptions = CreateHighlightColorOptions();
                        InitializeHighlightSettings();
                        InitializeDocumentResumeSettings();

                        _sections.CollectionChanged += OnSectionsCollectionChanged;
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
                                RaisePropertyChanged();
                                RaisePropertyChanged(nameof(IsDocumentLoaded));
                                RaisePropertyChanged(nameof(CanReadDocument));
                                RaisePropertyChanged(nameof(FileName));
                                UpdateCommandStates();
                                MarkDocumentMetadataDirty();
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
                                RaisePropertyChanged();
                                RaisePropertyChanged(nameof(HasPlainText));
                                RaisePropertyChanged(nameof(WordCount));
                                RaisePropertyChanged(nameof(CharacterCount));
                                RaisePropertyChanged(nameof(CanReadDocument));
                                UpdateCommandStates();
                                MarkDocumentMetadataDirty();
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
                                RaisePropertyChanged();
                                RaisePropertyChanged(nameof(FileName));
                                MarkDocumentMetadataDirty();
                        }
                }

                public string? FileName => string.IsNullOrWhiteSpace(FilePath) ? null : Path.GetFileName(FilePath);

                public bool IsDocumentLoaded => Document != null;

                public bool HasPlainText => !string.IsNullOrWhiteSpace(PlainText);

                public bool CanReadDocument => IsDocumentLoaded && HasPlainText;

                public bool RememberDocumentProgress
                {
                        get => _rememberDocumentProgress;
                        set
                        {
                                if (_rememberDocumentProgress == value)
                                        return;

                                _rememberDocumentProgress = value;
                                RaisePropertyChanged();

                                if (value)
                                {
                                        UpdateRememberDocumentSetting(true);
                                        PersistDocumentState(force: true);
                                }
                                else
                                {
                                        UpdateRememberDocumentSetting(false);
                                        _lastPersistedCharacterIndex = 0;
                                        _lastProgressSaveTime = DateTime.MinValue;
                                        _lastPersistedMetadata = null;
                                }
                        }
                }

                public ReadOnlyObservableCollection<DocumentSection> Sections => _readOnlySections;

                public bool HasSections => _sections.Count > 0;

                public DocumentSection? SelectedSection
                {
                        get => _selectedSection;
                        set
                        {
                                if (ReferenceEquals(_selectedSection, value))
                                        return;

                                _selectedSection = value;
                                RaisePropertyChanged();

                                if (_suppressSectionNavigation)
                                        return;

                                if (value != null)
                                        NavigateToSection(value);
                        }
                }

                public string PlaybackHotkeyCombination
                {
                        get => _playbackHotkeyCombination;
                        set
                        {
                                var newValue = value ?? string.Empty;
                                if (_playbackHotkeyCombination == newValue)
                                        return;

                                _playbackHotkeyCombination = newValue;
                                RaisePropertyChanged();
                                _applyPlaybackHotkeyCommand.RaiseCanExecuteChanged();
                        }
                }

                public bool PlaybackHotkeyTogglesPause
                {
                        get => _playbackHotkeyTogglesPause;
                        set
                        {
                                if (_playbackHotkeyTogglesPause == value)
                                        return;

                                _playbackHotkeyTogglesPause = value;
                                RaisePropertyChanged();
                                SavePlaybackHotkeySettings();
                        }
                }

                public Key PlaybackHotkeyKey => _playbackHotkeyKey;

                public ModifierKeys PlaybackHotkeyModifiers => _playbackHotkeyModifiers;

                public bool IsPlaying
                {
                        get => _isPlaying;
                        private set
                        {
                                if (_isPlaying == value)
                                        return;

                                _isPlaying = value;
                                RaisePropertyChanged();
                                RaisePropertyChanged(nameof(PlayPauseLabel));
                                _playbackHotkeyCommand.RaiseCanExecuteChanged();
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
                                RaisePropertyChanged();
                                RaisePropertyChanged(nameof(PlayPauseLabel));
                                _playbackHotkeyCommand.RaiseCanExecuteChanged();
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
                                RaisePropertyChanged();
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
                                RaisePropertyChanged();
                                if (!_suspendProgressPersistence)
                                        PersistDocumentState(force: false);
                        }
                }

                public string PlayPauseLabel => IsPlaying && !IsPaused ? "Pause" : "Play";

                public int WordCount => _plainText?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

                public int CharacterCount => _plainText?.Length ?? 0;

                public IReadOnlyList<HighlightColorOption> HighlightColorOptions => _highlightColorOptions;

                public HighlightColorOption SelectedHighlightColor
                {
                        get => _selectedHighlightColor;
                        set
                        {
                                if (value == null)
                                        value = _highlightColorOptions.First();

                                if (ReferenceEquals(_selectedHighlightColor, value))
                                {
                                        UpdateHighlightBrush();
                                        return;
                                }

                                SetSelectedHighlightColor(value, true);
                        }
                }

                public Brush HighlightBrush => _highlightBrush;

                public int HighlightStartIndex
                {
                        get => _highlightStartIndex;
                        private set
                        {
                                if (_highlightStartIndex == value)
                                        return;

                                _highlightStartIndex = value;
                                RaisePropertyChanged();
                        }
                }

                public int HighlightLength
                {
                        get => _highlightLength;
                        private set
                        {
                                if (_highlightLength == value)
                                        return;

                                _highlightLength = value;
                                RaisePropertyChanged();
                        }
                }

                public bool IsBusy
                {
                        get => _isBusy;
                        private set
                        {
                                if (_isBusy == value)
                                        return;

                                _isBusy = value;
                                RaisePropertyChanged();
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
                                RaisePropertyChanged();
                                RaisePropertyChanged(nameof(HasStatusMessage));
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
                                RaisePropertyChanged();
                        }
                }

                public ICommand ClearDocumentCommand => _clearDocumentCommand;

                public ICommand BrowseForDocumentCommand => _browseForDocumentCommand;

                public ICommand PlayPauseCommand => _playPauseCommand;

                public ICommand SeekBackwardCommand => _seekBackwardCommand;

                public ICommand SeekForwardCommand => _seekForwardCommand;

                public ICommand ApplyPlaybackHotkeyCommand => _applyPlaybackHotkeyCommand;

                public ICommand PlaybackHotkeyCommand => _playbackHotkeyCommand;

                public async Task<bool> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default, bool persistImmediately = true)
                {
                        if (string.IsNullOrWhiteSpace(filePath))
                                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

                        try
                        {
                                IsBusy = true;
                                LastError = null;
                                StatusMessage = null;

                                var result = await _documentReaderService.ReadDocumentAsync(filePath, cancellationToken);

                                ApplyResult(result, persistImmediately);
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
                        _currentDocumentMetadata = null;
                        _lastPersistedMetadata = null;
                        _documentMetadataDirty = true;
                        _lastPersistedCharacterIndex = 0;
                        _sections.Clear();
                        SelectedSection = null;
                        ResetPlaybackState();
                        if (RememberDocumentProgress)
                                ClearPersistedDocumentState();
                }

                private async void BrowseForDocument()
                {
                        var dialog = new OpenFileDialog
                        {
                                Filter = "Documents (*.epub;*.pdf;*.txt)|*.epub;*.pdf;*.txt|EPUB files (*.epub)|*.epub|PDF files (*.pdf)|*.pdf|Text files (*.txt)|*.txt|All files (*.*)|*.*",
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

                private void ApplyResult(DocumentReadResult result, bool persistImmediately)
                {
                        if (result == null)
                                throw new ArgumentNullException(nameof(result));

                        Document = result.Document ?? CreateFlowDocument(result.PlainText);
                        PlainText = result.PlainText;
                        FilePath = result.FilePath;
                        UpdateSections(result.Sections);
                        LastError = null;
                        ResetPlaybackState();
                        EnsureDocumentMetadata();
                        if (persistImmediately)
                                PersistDocumentState(force: true);
                }

                private void UpdateSections(IReadOnlyList<DocumentSection>? sections)
                {
                        _suppressSectionNavigation = true;
                        try
                        {
                                _sections.Clear();
                                if (sections != null)
                                {
                                        foreach (var section in sections)
                                                _sections.Add(section);
                                }

                                SelectedSection = null;
                        }
                        finally
                        {
                                _suppressSectionNavigation = false;
                        }
                }

                private void OnSectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
                {
                        RaisePropertyChanged(nameof(HasSections));
                }

                private void NavigateToSection(DocumentSection section)
                {
                        if (section == null || PlainText == null)
                                return;

                        var index = Math.Clamp(section.StartCharacterIndex, 0, PlainText.Length);
                        var targetTime = EstimateTimeFromCharacterIndex(index);

                        CurrentCharacterIndex = index;
                        CurrentAudioPosition = targetTime;
                        SetHighlightRange(index, 1);
                        SetHighlightRange(index, 0);

                        if (IsPlaying)
                        {
                                _pendingSeekCharacterIndex = index;
                                _pendingSeekAudioPosition = targetTime;
                                _isStoppingForSeek = true;
                                IsPlaying = false;
                                _ttsService.Stop();
                        }
                }

                private void UpdateCommandStates()
                {
                        _clearDocumentCommand.RaiseCanExecuteChanged();
                        _browseForDocumentCommand.RaiseCanExecuteChanged();
                        _playPauseCommand.RaiseCanExecuteChanged();
                        _seekBackwardCommand.RaiseCanExecuteChanged();
                        _seekForwardCommand.RaiseCanExecuteChanged();
                        _playbackHotkeyCommand.RaiseCanExecuteChanged();
                        _applyPlaybackHotkeyCommand.RaiseCanExecuteChanged();
                }

                private void InitializePlaybackHotkeyFromSettings()
                {
                        var settings = _settingsService.GetCurrentSettings();
                        settings.DocumentReaderHotkey ??= new AppSettings.DocumentReaderHotkeySettings();

                        _playbackHotkeyTogglesPause = settings.DocumentReaderHotkey.UsePlayPauseToggle;

                        var combination = ComposePlaybackHotkeyString(settings.DocumentReaderHotkey);
                        _playbackHotkeyCombination = combination;
                        _lastAppliedPlaybackHotkeyCombination = combination;

                        if (!TryParsePlaybackHotkey(combination, out var modifiers, out var key))
                        {
                                modifiers = ModifierKeys.None;
                                key = Key.None;
                        }

                        _playbackHotkeyModifiers = modifiers;
                        _playbackHotkeyKey = key;

                        RaisePropertyChanged(nameof(PlaybackHotkeyCombination));
                        RaisePropertyChanged(nameof(PlaybackHotkeyKey));
                        RaisePropertyChanged(nameof(PlaybackHotkeyModifiers));
                        RaisePropertyChanged(nameof(PlaybackHotkeyTogglesPause));
                        _playbackHotkeyCommand.RaiseCanExecuteChanged();
                        _applyPlaybackHotkeyCommand.RaiseCanExecuteChanged();
                }

                private void InitializeHighlightSettings()
                {
                        var settings = _settingsService.GetCurrentSettings();
                        var selectedId = settings.DocumentReaderHighlightColor;
                        var option = _highlightColorOptions.FirstOrDefault(o => string.Equals(o.Id, selectedId, StringComparison.Ordinal))
                                     ?? _highlightColorOptions.First();

                        SetSelectedHighlightColor(option, false);
                }

                private bool CanApplyPlaybackHotkey()
                {
                        if (IsBusy)
                                return false;

                        var combination = _playbackHotkeyCombination?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(combination))
                                return !string.IsNullOrEmpty(_lastAppliedPlaybackHotkeyCombination);

                        if (!TryParsePlaybackHotkey(combination, out var modifiers, out var key))
                                return false;

                        var canonical = FormatHotkey(modifiers, key);
                        return !string.Equals(canonical, _lastAppliedPlaybackHotkeyCombination, StringComparison.Ordinal);
                }

                private void ApplyPlaybackHotkey()
                {
                        var combination = _playbackHotkeyCombination?.Trim() ?? string.Empty;

                        if (string.IsNullOrEmpty(combination))
                        {
                                _playbackHotkeyKey = Key.None;
                                _playbackHotkeyModifiers = ModifierKeys.None;
                                _playbackHotkeyCombination = string.Empty;
                                _lastAppliedPlaybackHotkeyCombination = string.Empty;

                                RaisePropertyChanged(nameof(PlaybackHotkeyCombination));
                                RaisePropertyChanged(nameof(PlaybackHotkeyKey));
                                RaisePropertyChanged(nameof(PlaybackHotkeyModifiers));

                                SavePlaybackHotkeySettings();
                                _applyPlaybackHotkeyCommand.RaiseCanExecuteChanged();
                                return;
                        }

                        if (!TryParsePlaybackHotkey(combination, out var modifiers, out var key))
                                return;

                        var canonical = FormatHotkey(modifiers, key);

                        if (_playbackHotkeyCombination != canonical)
                        {
                                _playbackHotkeyCombination = canonical;
                                RaisePropertyChanged(nameof(PlaybackHotkeyCombination));
                        }

                        _playbackHotkeyModifiers = modifiers;
                        _playbackHotkeyKey = key;
                        _lastAppliedPlaybackHotkeyCombination = canonical;

                        RaisePropertyChanged(nameof(PlaybackHotkeyKey));
                        RaisePropertyChanged(nameof(PlaybackHotkeyModifiers));

                        SavePlaybackHotkeySettings();
                        _applyPlaybackHotkeyCommand.RaiseCanExecuteChanged();
                }

                private void ExecutePlaybackHotkey()
                {
                        if (!HasPlainText && !IsPlaying && !IsPaused)
                                return;

                        if (PlaybackHotkeyTogglesPause)
                        {
                                TogglePlaybackInternal();
                                return;
                        }

                        if (IsPlaying || IsPaused)
                        {
                                ResetPlaybackState();
                        }
                        else
                        {
                                StartPlaybackFromCurrentPosition();
                        }
                }

                private void SavePlaybackHotkeySettings()
                {
                        var settings = _settingsService.GetCurrentSettings();
                        settings.DocumentReaderHotkey ??= new AppSettings.DocumentReaderHotkeySettings();

                        settings.DocumentReaderHotkey.Modifiers = ComposeModifierString(_playbackHotkeyModifiers);
                        settings.DocumentReaderHotkey.Key = _playbackHotkeyKey == Key.None ? string.Empty : _playbackHotkeyKey.ToString();
                        settings.DocumentReaderHotkey.UsePlayPauseToggle = _playbackHotkeyTogglesPause;

                        _settingsService.SaveCurrentSettings();
                }

                private static string ComposeModifierString(ModifierKeys modifiers)
                {
                        var parts = new List<string>();

                        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                parts.Add("Ctrl");

                        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                                parts.Add("Shift");

                        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                                parts.Add("Alt");

                        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                                parts.Add("Win");

                        return string.Join("+", parts);
                }

                private static string FormatHotkey(ModifierKeys modifiers, Key key)
                {
                        var parts = new List<string>();

                        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                parts.Add("Ctrl");

                        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                                parts.Add("Shift");

                        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                                parts.Add("Alt");

                        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                                parts.Add("Win");

                        parts.Add(key.ToString());

                        return string.Join("+", parts);
                }

                private static string ComposePlaybackHotkeyString(AppSettings.DocumentReaderHotkeySettings? hotkey)
                {
                        if (hotkey == null || string.IsNullOrWhiteSpace(hotkey.Key))
                                return string.Empty;

                        var keyPart = hotkey.Key.Trim();
                        if (string.IsNullOrWhiteSpace(hotkey.Modifiers))
                                return keyPart;

                        var candidate = hotkey.Modifiers.Trim() + "+" + keyPart;
                        return TryParsePlaybackHotkey(candidate, out var modifiers, out var key)
                                ? FormatHotkey(modifiers, key)
                                : candidate;
                }

                private static bool TryParsePlaybackHotkey(string combination, out ModifierKeys modifiers, out Key key)
                {
                        modifiers = ModifierKeys.None;
                        key = Key.None;

                        if (string.IsNullOrWhiteSpace(combination))
                                return false;

                        var rawParts = combination.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                        if (rawParts.Length == 0)
                                return false;

                        var parts = new List<string>();
                        foreach (var raw in rawParts)
                        {
                                var trimmed = raw.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                        parts.Add(trimmed);
                        }

                        if (parts.Count == 0)
                                return false;

                        var keyCandidate = parts[parts.Count - 1];
                        if (!Enum.TryParse(keyCandidate, true, out key) || key == Key.None)
                                return false;

                        modifiers = ModifierKeys.None;
                        for (var i = 0; i < parts.Count - 1; i++)
                        {
                                if (!TryParseModifier(parts[i], out var modifier))
                                        return false;

                                modifiers |= modifier;
                        }

                        return true;
                }

                private static bool TryParseModifier(string candidate, out ModifierKeys modifier)
                {
                        if (string.IsNullOrWhiteSpace(candidate))
                        {
                                modifier = ModifierKeys.None;
                                return false;
                        }

                        switch (candidate.Trim().ToLowerInvariant())
                        {
                                case "ctrl":
                                case "control":
                                        modifier = ModifierKeys.Control;
                                        return true;
                                case "shift":
                                        modifier = ModifierKeys.Shift;
                                        return true;
                                case "alt":
                                        modifier = ModifierKeys.Alt;
                                        return true;
                                case "win":
                                case "windows":
                                case "cmd":
                                case "command":
                                case "meta":
                                        modifier = ModifierKeys.Windows;
                                        return true;
                                default:
                                        modifier = ModifierKeys.None;
                                        return false;
                        }
                }

                private static FlowDocument CreateFlowDocument(string content)
                {
                        var document = new FlowDocument();

                        if (string.IsNullOrEmpty(content))
                                return document;

                        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
                        var paragraphs = normalized.Split(new[] { "\n\n" }, StringSplitOptions.None);

                        foreach (var paragraphText in paragraphs)
                        {
                                var paragraph = new Paragraph();
                                var lines = paragraphText.Split('\n');

                                for (var i = 0; i < lines.Length; i++)
                                {
                                        if (i > 0)
                                                paragraph.Inlines.Add(new LineBreak());

                                        paragraph.Inlines.Add(new Run(lines[i]));
                                }

                                document.Blocks.Add(paragraph);
                        }

                        return document;
                }

                private void TogglePlayback()
                {
                        if (!CanReadDocument)
                                return;

                        TogglePlaybackInternal();
                }

                private void TogglePlaybackInternal()
                {
                        if (!HasPlainText && !IsPlaying && !IsPaused)
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
                        SetHighlightRange(CurrentCharacterIndex, 0);
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
                        SetHighlightRange(CurrentCharacterIndex, 0);

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
                        var previousSuppressed = _suspendProgressPersistence;
                        _suspendProgressPersistence = true;
                        CurrentCharacterIndex = 0;
                        CurrentAudioPosition = TimeSpan.Zero;
                        _suspendProgressPersistence = previousSuppressed;
                        SetHighlightRange(0, 0);
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
                        if (_currentPrompt == null || !ReferenceEquals(e.Prompt, _currentPrompt))
                                return;

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

                        var highlightStart = Math.Clamp(_playbackStartCharacterIndex + e.CharacterPosition, 0, PlainText.Length);
                        var highlightLength = Math.Clamp(e.CharacterCount, 0, PlainText.Length - highlightStart);
                        SetHighlightRange(highlightStart, highlightLength);
                }

                private void OnSpeechCompleted(object? sender, SpeakCompletedEventArgs e)
                {
                        if (_currentPrompt == null || !ReferenceEquals(e.Prompt, _currentPrompt))
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
                                SetHighlightRange(CurrentCharacterIndex, 0);
                                return;
                        }

                        IsPlaying = false;
                        IsPaused = false;

                        if (PlainText != null)
                        {
                                CurrentCharacterIndex = PlainText.Length;
                                CurrentAudioPosition = EstimateTimeFromCharacterIndex(CurrentCharacterIndex);
                                SetHighlightRange(CurrentCharacterIndex, 0);
                        }
                }

                public void RefreshHighlightBrush()
                {
                        UpdateHighlightBrush();
                }

                public void ReloadHighlightSettings()
                {
                        InitializeHighlightSettings();
                }

                private void InitializeDocumentResumeSettings()
                {
                        var settings = _settingsService.GetCurrentSettings();

                        _rememberDocumentProgress = settings.RememberDocumentReaderPosition;
                        _lastPersistedCharacterIndex = Math.Max(0, settings.DocumentReaderLastCharacterIndex);
                        _lastPersistedMetadata = null;
                        _lastProgressSaveTime = DateTime.MinValue;
                        RaisePropertyChanged(nameof(RememberDocumentProgress));

                        if (!RememberDocumentProgress)
                                return;

                        var resumeState = settings.DocumentReaderResumeState;
                        if (resumeState == null && !string.IsNullOrWhiteSpace(settings.DocumentReaderLastFilePath))
                        {
                                resumeState = new AppSettings.DocumentReaderResumeSnapshot
                                {
                                        FilePath = settings.DocumentReaderLastFilePath,
                                        CharacterIndex = Math.Max(0, settings.DocumentReaderLastCharacterIndex),
                                        DocumentLength = 0,
                                };
                        }

                        if (resumeState == null || string.IsNullOrWhiteSpace(resumeState.FilePath))
                                return;

                        if (!File.Exists(resumeState.FilePath))
                        {
                                StatusMessage = "The previously saved document could not be found.";
                                ClearPersistedDocumentState();
                                return;
                        }

                        RestoreDocumentFromSettings(resumeState);
                }

                private void PersistDocumentState(bool force)
                {
                        if (_suspendProgressPersistence && !force)
                                return;

                        if (!RememberDocumentProgress)
                                return;

                        if (string.IsNullOrWhiteSpace(FilePath) || PlainText == null)
                                return;

                        var metadata = EnsureDocumentMetadata();
                        if (metadata == null)
                                return;

                        var normalizedIndex = Math.Clamp(CurrentCharacterIndex, 0, metadata.DocumentLength);
                        var now = DateTime.UtcNow;

                        if (!force)
                        {
                                if (normalizedIndex == _lastPersistedCharacterIndex && MetadataEquals(_lastPersistedMetadata, metadata))
                                {
                                        if (now - _lastProgressSaveTime < ProgressPersistInterval)
                                                return;
                                }
                                else if (now - _lastProgressSaveTime < ProgressPersistInterval)
                                {
                                        return;
                                }
                        }

                        var state = new AppSettings.DocumentReaderResumeSnapshot
                        {
                                FilePath = metadata.FilePath,
                                CharacterIndex = normalizedIndex,
                                DocumentLength = metadata.DocumentLength,
                                ContentHash = metadata.ContentHash,
                                FileSize = metadata.FileSize,
                                LastWriteTimeUtc = metadata.LastWriteTimeUtc,
                        };

                        var settings = _settingsService.GetCurrentSettings();
                        settings.DocumentReaderResumeState = state;
                        settings.DocumentReaderLastFilePath = state.FilePath;
                        settings.DocumentReaderLastCharacterIndex = state.CharacterIndex;
                        settings.RememberDocumentReaderPosition = true;
                        _settingsService.SaveCurrentSettings();

                        _lastPersistedCharacterIndex = state.CharacterIndex;
                        _lastProgressSaveTime = now;
                        _lastPersistedMetadata = metadata;
                }

                private void ClearPersistedDocumentState()
                {
                        var settings = _settingsService.GetCurrentSettings();
                        settings.DocumentReaderResumeState = null;
                        settings.DocumentReaderLastFilePath = null;
                        settings.DocumentReaderLastCharacterIndex = 0;
                        _settingsService.SaveCurrentSettings();

                        _lastPersistedCharacterIndex = 0;
                        _lastProgressSaveTime = DateTime.MinValue;
                        _lastPersistedMetadata = null;
                }

                private void UpdateRememberDocumentSetting(bool value)
                {
                        var settings = _settingsService.GetCurrentSettings();
                        settings.RememberDocumentReaderPosition = value;

                        if (!value)
                        {
                                settings.DocumentReaderResumeState = null;
                                settings.DocumentReaderLastFilePath = null;
                                settings.DocumentReaderLastCharacterIndex = 0;
                        }

                        _settingsService.SaveCurrentSettings();
                }

                private async void RestoreDocumentFromSettings(AppSettings.DocumentReaderResumeSnapshot resumeState)
                {
                        try
                        {
                                if (resumeState == null || string.IsNullOrWhiteSpace(resumeState.FilePath))
                                {
                                        ClearPersistedDocumentState();
                                        return;
                                }

                                if (!File.Exists(resumeState.FilePath))
                                {
                                        StatusMessage = "The previously saved document could not be found.";
                                        ClearPersistedDocumentState();
                                        return;
                                }

                                var loaded = await LoadDocumentAsync(resumeState.FilePath, cancellationToken: default, persistImmediately: false);
                                if (!loaded || PlainText == null)
                                        return;

                                var metadata = EnsureDocumentMetadata();
                                if (metadata == null)
                                        return;

                                var isCompatible = IsMetadataCompatible(resumeState, metadata);
                                var clamped = Math.Clamp(resumeState.CharacterIndex, 0, metadata.DocumentLength);

                                var previousSuppressed = _suspendProgressPersistence;
                                _suspendProgressPersistence = true;

                                try
                                {
                                        if (isCompatible && clamped > 0)
                                        {
                                                CurrentCharacterIndex = clamped;
                                                CurrentAudioPosition = EstimateTimeFromCharacterIndex(clamped);
                                        }
                                        else
                                        {
                                                CurrentCharacterIndex = 0;
                                                CurrentAudioPosition = TimeSpan.Zero;
                                                if (!isCompatible)
                                                        StatusMessage = "The previously saved document has changed. Progress was reset.";
                                        }

                                        SetHighlightRange(CurrentCharacterIndex, 0);
                                }
                                finally
                                {
                                        _suspendProgressPersistence = previousSuppressed;
                                }

                                _lastPersistedCharacterIndex = CurrentCharacterIndex;
                                _lastProgressSaveTime = DateTime.UtcNow;
                                _lastPersistedMetadata = metadata;
                                PersistDocumentState(force: true);
                        }
                        catch (OperationCanceledException)
                        {
                                return;
                        }
                        catch (Exception ex)
                        {
                                LastError = ex;
                                StatusMessage = "Failed to restore the previously opened document.";
                                ClearPersistedDocumentState();
                        }
                }

                private void MarkDocumentMetadataDirty()
                {
                        _documentMetadataDirty = true;
                        _currentDocumentMetadata = null;
                }

                private DocumentMetadata? EnsureDocumentMetadata()
                {
                        if (!_documentMetadataDirty && _currentDocumentMetadata != null)
                                return _currentDocumentMetadata;

                        if (PlainText == null || string.IsNullOrWhiteSpace(FilePath))
                        {
                                _currentDocumentMetadata = null;
                                _documentMetadataDirty = false;
                                return null;
                        }

                        var metadata = new DocumentMetadata
                        {
                                FilePath = FilePath,
                                DocumentLength = PlainText.Length,
                                ContentHash = ComputeContentHash(PlainText),
                        };

                        try
                        {
                                var info = new FileInfo(FilePath);
                                if (info.Exists)
                                {
                                        metadata.FileSize = info.Length;
                                        metadata.LastWriteTimeUtc = info.LastWriteTimeUtc;
                                }
                        }
                        catch (Exception)
                        {
                                metadata.FileSize = null;
                                metadata.LastWriteTimeUtc = null;
                        }

                        _currentDocumentMetadata = metadata;
                        _documentMetadataDirty = false;
                        return metadata;
                }

                private static bool MetadataEquals(DocumentMetadata? left, DocumentMetadata? right)
                {
                        if (left == null || right == null)
                                return false;

                        if (!string.Equals(left.FilePath, right.FilePath, StringComparison.OrdinalIgnoreCase))
                                return false;

                        if (left.DocumentLength != right.DocumentLength)
                                return false;

                        if (!string.Equals(left.ContentHash, right.ContentHash, StringComparison.Ordinal))
                                return false;

                        if (left.FileSize != right.FileSize)
                                return false;

                        if (!NullableDateTimeEquals(left.LastWriteTimeUtc, right.LastWriteTimeUtc))
                                return false;

                        return true;
                }

                private static bool NullableDateTimeEquals(DateTime? first, DateTime? second)
                {
                        if (!first.HasValue && !second.HasValue)
                                return true;

                        if (!first.HasValue || !second.HasValue)
                                return false;

                        return Math.Abs((first.Value - second.Value).TotalSeconds) <= 1;
                }

                private static bool IsMetadataCompatible(AppSettings.DocumentReaderResumeSnapshot expected, DocumentMetadata actual)
                {
                        if (expected == null)
                                return false;

                        if (!string.Equals(expected.FilePath, actual.FilePath, StringComparison.OrdinalIgnoreCase))
                                return false;

                        if (expected.DocumentLength > 0 && expected.DocumentLength != actual.DocumentLength)
                                return false;

                        if (!string.IsNullOrEmpty(expected.ContentHash) && !string.Equals(expected.ContentHash, actual.ContentHash, StringComparison.Ordinal))
                                return false;

                        if (expected.FileSize.HasValue && actual.FileSize.HasValue && expected.FileSize.Value != actual.FileSize.Value)
                                return false;

                        if (expected.LastWriteTimeUtc.HasValue && actual.LastWriteTimeUtc.HasValue)
                        {
                                if (!NullableDateTimeEquals(expected.LastWriteTimeUtc, actual.LastWriteTimeUtc))
                                        return false;
                        }

                        return true;
                }

                private static string ComputeContentHash(string content)
                {
                        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
                        var hash = SHA256.HashData(bytes);
                        return Convert.ToHexString(hash);
                }

                private sealed class DocumentMetadata
                {
                        public string FilePath { get; set; } = string.Empty;

                        public int DocumentLength { get; set; }

                        public string ContentHash { get; set; } = string.Empty;

                        public long? FileSize { get; set; }

                        public DateTime? LastWriteTimeUtc { get; set; }
                }

                private IReadOnlyList<HighlightColorOption> CreateHighlightColorOptions()
                {
                        return new ReadOnlyCollection<HighlightColorOption>(new List<HighlightColorOption>
                        {
                                new HighlightColorOption(ThemeAccentHighlightId, "Theme accent", null, "AccentBrush"),
                                new HighlightColorOption("Warm", "Warm orange", Color.FromRgb(0xFF, 0x9E, 0x57)),
                                new HighlightColorOption("Cool", "Cool blue", Color.FromRgb(0x42, 0xA5, 0xF5)),
                                new HighlightColorOption("Calm", "Calm green", Color.FromRgb(0x66, 0xBB, 0x6A)),
                                new HighlightColorOption("Violet", "Vibrant violet", Color.FromRgb(0xAB, 0x47, 0xBC)),
                        });
                }

                private void SetSelectedHighlightColor(HighlightColorOption option, bool persist)
                {
                        var newOption = option ?? _highlightColorOptions.First();

                        if (!ReferenceEquals(_selectedHighlightColor, newOption))
                        {
                                _selectedHighlightColor = newOption;
                                RaisePropertyChanged(nameof(SelectedHighlightColor));
                        }

                        UpdateHighlightBrush();

                        if (persist)
                        {
                                var settings = _settingsService.GetCurrentSettings();
                                if (!string.Equals(settings.DocumentReaderHighlightColor, newOption.Id, StringComparison.Ordinal))
                                {
                                        settings.DocumentReaderHighlightColor = newOption.Id;
                                        _settingsService.SaveCurrentSettings();
                                }
                        }
                }

                private void UpdateHighlightBrush()
                {
                        var brush = ResolveBrushForOption(_selectedHighlightColor);

                        if (!ReferenceEquals(_highlightBrush, brush))
                        {
                                _highlightBrush = brush;
                                RaisePropertyChanged(nameof(HighlightBrush));
                        }
                        else
                        {
                                RaisePropertyChanged(nameof(HighlightBrush));
                        }
                }

                private static Brush ResolveBrushForOption(HighlightColorOption option)
                {
                        if (option != null && !string.IsNullOrWhiteSpace(option.ResourceKey))
                        {
                                if (Application.Current?.TryFindResource(option.ResourceKey) is Brush resourceBrush)
                                        return resourceBrush;
                        }

                        if (option != null && option.Color.HasValue)
                        {
                                var solid = new SolidColorBrush(option.Color.Value);
                                if (solid.CanFreeze)
                                        solid.Freeze();
                                return solid;
                        }

                        return Brushes.Transparent;
                }

                private void SetHighlightRange(int startIndex, int length)
                {
                        var totalLength = PlainText?.Length ?? 0;
                        if (totalLength <= 0)
                        {
                                HighlightStartIndex = 0;
                                HighlightLength = 0;
                                return;
                        }

                        var clampedStart = Math.Clamp(startIndex, 0, totalLength);
                        var available = totalLength - clampedStart;
                        var clampedLength = Math.Clamp(length, 0, available);

                        HighlightStartIndex = clampedStart;
                        HighlightLength = clampedLength;
                }

                private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
                {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }

                public sealed class HighlightColorOption
                {
                        public HighlightColorOption(string id, string displayName, Color? color = null, string? resourceKey = null)
                        {
                                Id = id ?? throw new ArgumentNullException(nameof(id));
                                DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
                                Color = color;
                                ResourceKey = resourceKey;
                        }

                        public string Id { get; }

                        public string DisplayName { get; }

                        public Color? Color { get; }

                        public string? ResourceKey { get; }
                }
        }
}
