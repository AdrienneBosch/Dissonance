using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Dissonance.Infrastructure.Commands;
using Dissonance.Infrastructure.Constants;
using Dissonance.Managers;
using Dissonance.Services.HotkeyService;
using Dissonance.Services.MessageService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.StatusAnnouncements;
using Dissonance.Services.ThemeService;
using Dissonance.Services.TTSService;

using Microsoft.Win32;

using NLog;

namespace Dissonance.ViewModels
{
        public class MainWindowViewModel : INotifyPropertyChanged
        {
                private const string ConfigurationFileFilter = "Dissonance Configuration (*.json)|*.json|All files (*.*)|*.*";
                private const string DefaultExportFileName = "dissonance-config.json";
                private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
                private readonly IHotkeyService _hotkeyService;
                private readonly IMessageService _messageService;
                private readonly ISettingsService _settingsService;
                private readonly ITTSService _ttsService;
                private readonly IThemeService _themeService;
                private readonly ClipboardManager _clipboardManager;
                private readonly IStatusAnnouncementService _statusAnnouncementService;
                private readonly Dispatcher _dispatcher;
                private readonly ObservableCollection<NavigationSectionViewModel> _navigationSections = new ObservableCollection<NavigationSectionViewModel> ( );
                private readonly ObservableCollection<StatusAnnouncement> _statusHistory = new ObservableCollection<StatusAnnouncement> ( );
                private readonly ReadOnlyObservableCollection<StatusAnnouncement> _statusHistoryView;
                private bool _isDarkTheme;
                private bool _isNavigationMenuOpen;
                private string _hotkeyCombination = string.Empty;
                private string _lastAppliedHotkeyCombination = string.Empty;
                private bool _autoReadClipboard;
                private NavigationSectionViewModel? _selectedSection;
                private readonly string _previewStartLabel;
                private readonly string _previewStopLabel;
                private readonly string _previewToolTip;
                private readonly string _previewHelpText;
                private readonly string _previewSampleText;
                private Prompt? _activePreviewPrompt;
                private bool _isPreviewing;
                private StatusAnnouncement? _latestStatus;

                private const int MaxStatusItems = 100;

                public MainWindowViewModel ( ISettingsService settingsService, ITTSService ttsService, IHotkeyService hotkeyService, IThemeService themeService, IMessageService messageService, ClipboardManager clipboardManager, IStatusAnnouncementService statusAnnouncementService )
                {
                        _settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
                        _ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );
                        _hotkeyService = hotkeyService ?? throw new ArgumentNullException ( nameof ( hotkeyService ) );
                        _themeService = themeService ?? throw new ArgumentNullException ( nameof ( themeService ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
                        _clipboardManager = clipboardManager ?? throw new ArgumentNullException ( nameof ( clipboardManager ) );
                        _statusAnnouncementService = statusAnnouncementService ?? throw new ArgumentNullException ( nameof ( statusAnnouncementService ) );

                        _statusHistoryView = new ReadOnlyObservableCollection<StatusAnnouncement> ( _statusHistory );

                        foreach ( var entry in _statusAnnouncementService.History )
                        {
                                _statusHistory.Add ( entry );
                        }

                        _latestStatus = _statusAnnouncementService.Latest;
                        TrimStatusHistory ( );
                        if ( _latestStatus != null )
                        {
                                OnPropertyChanged ( nameof ( StatusMessage ) );
                                OnPropertyChanged ( nameof ( StatusSeverity ) );
                                OnPropertyChanged ( nameof ( IsStatusMessageVisible ) );
                        }

                        _statusAnnouncementService.StatusAnnounced += OnStatusAnnounced;

                        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

                        SaveDefaultSettingsCommand = new RelayCommandNoParam ( SaveCurrentConfigurationAsDefault );
                        ExportSettingsCommand = new RelayCommandNoParam ( ExportConfiguration );
                        ImportSettingsCommand = new RelayCommandNoParam ( ImportConfiguration );
                        ApplyHotkeyCommand = new RelayCommandNoParam ( ApplyHotkey, CanApplyHotkey );
                        NavigateToSectionCommand = new RelayCommand ( NavigateToSection );

                        _previewStartLabel = GetResourceString ( "PreviewVoiceButtonLabelStart", "Preview voice" );
                        _previewStopLabel = GetResourceString ( "PreviewVoiceButtonLabelStop", "Stop preview" );
                        _previewToolTip = GetResourceString ( "PreviewVoiceButtonToolTip", "Listen to how the selected voice settings sound together." );
                        _previewHelpText = GetResourceString ( "PreviewVoiceHelpText", "Play or stop a short professional sample using the selected voice." );
                        _previewSampleText = GetResourceString ( "PreviewVoiceSampleSentence", "Dissonance narrates your copied content so you can stay focused on your work." );

                        PreviewVoiceCommand = new RelayCommandNoParam ( PreviewVoice, ( ) => !string.IsNullOrWhiteSpace ( _previewSampleText ) );

                        var installedVoices = new System.Speech.Synthesis.SpeechSynthesizer ( ).GetInstalledVoices ( );
                        foreach ( var voice in installedVoices )
                        {
                                AvailableVoices.Add ( voice.VoiceInfo.Name );
                        }

                        var settings = _settingsService.GetCurrentSettings ( );
                        _hotkeyCombination = ComposeHotkeyString ( settings.Hotkey );
                        _lastAppliedHotkeyCombination = _hotkeyCombination;
                        _autoReadClipboard = settings.Hotkey?.AutoReadClipboard ?? false;
                        _clipboardManager.SetAutoReadClipboard ( _autoReadClipboard );

                        if ( !string.IsNullOrWhiteSpace ( _hotkeyCombination ) )
                        {
                                try
                                {
                                        UpdateHotkey ( _hotkeyCombination );
                                        _lastAppliedHotkeyCombination = _hotkeyCombination;
                                }
                                catch ( ArgumentException ex )
                                {
                                        Logger.Warn ( ex, "Invalid hotkey configuration \"{Hotkey}\" loaded from settings.", _hotkeyCombination );
                                        PublishStatus ( "StatusMessageHotkeyConfigurationInvalid", "The saved hotkey couldn't be restored.", StatusSeverity.Warning );
                                }
                        }

                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
                        _ttsService.SpeechCompleted += OnSpeechCompleted;

                        _isDarkTheme = settings.UseDarkTheme;
                        _themeService.ApplyTheme ( _isDarkTheme ? AppTheme.Dark : AppTheme.Light );
                        settings.UseDarkTheme = _isDarkTheme;
                        OnPropertyChanged ( nameof ( IsDarkTheme ) );
                        OnPropertyChanged ( nameof ( CurrentThemeName ) );
                        OnPropertyChanged ( nameof ( SaveConfigAsDefaultOnClose ) );

                        _navigationSections.Add ( new NavigationSectionViewModel (
                                "clipboard-reader",
                                "Clipboard Reader",
                                "Instantly speak the text you've copied to the clipboard.",
                                "Clipboard Reader",
                                "Fine-tune speech playback, volume, and shortcuts for the clipboard narration experience.",
                                this,
                                showSettingsControls: true ) );
                }

                public event PropertyChangedEventHandler PropertyChanged;

                public ObservableCollection<string> AvailableVoices { get; } = new ObservableCollection<string> ( );

                public ObservableCollection<NavigationSectionViewModel> NavigationSections => _navigationSections;

                public ReadOnlyObservableCollection<StatusAnnouncement> StatusHistory => _statusHistoryView;

                public string? StatusMessage => _latestStatus?.Message;

                public StatusSeverity StatusSeverity => _latestStatus?.Severity ?? StatusSeverity.Info;

                public bool IsStatusMessageVisible => !string.IsNullOrWhiteSpace ( StatusMessage );

                public ICommand ApplyHotkeyCommand { get; }

                public ICommand ExportSettingsCommand { get; }

                public ICommand ImportSettingsCommand { get; }

                public ICommand SaveDefaultSettingsCommand { get; }

                public ICommand NavigateToSectionCommand { get; }

                public ICommand PreviewVoiceCommand { get; }

                public bool IsPreviewing
                {
                        get => _isPreviewing;
                        private set
                        {
                                if ( _isPreviewing == value )
                                        return;

                                _isPreviewing = value;
                                OnPropertyChanged ( nameof ( IsPreviewing ) );
                                OnPropertyChanged ( nameof ( PreviewVoiceButtonLabel ) );
                        }
                }

                public string PreviewVoiceButtonLabel => IsPreviewing ? _previewStopLabel : _previewStartLabel;

                public string PreviewVoiceButtonToolTip => _previewToolTip;

                public string PreviewVoiceHelpText => _previewHelpText;

                public bool IsNavigationMenuOpen
                {
                        get => _isNavigationMenuOpen;
                        set
                        {
                                if ( _isNavigationMenuOpen == value )
                                        return;

                                _isNavigationMenuOpen = value;
                                OnPropertyChanged ( nameof ( IsNavigationMenuOpen ) );
                        }
                }

                public NavigationSectionViewModel? SelectedSection
                {
                        get => _selectedSection;
                        set
                        {
                                if ( _selectedSection == value )
                                        return;

                                _selectedSection = value;
                                OnPropertyChanged ( nameof ( SelectedSection ) );
                                OnPropertyChanged ( nameof ( IsHomeSelected ) );
                                if ( value != null )
                                {
                                        IsNavigationMenuOpen = false;
                                }
                        }
                }

                public bool IsHomeSelected => SelectedSection == null;

                public bool IsDarkTheme
                {
                        get => _isDarkTheme;
                        set
                        {
                                if ( _isDarkTheme == value )
                                        return;

                                _isDarkTheme = value;
                                _themeService.ApplyTheme ( value ? AppTheme.Dark : AppTheme.Light );
                                var settings = _settingsService.GetCurrentSettings ( );
                                if ( settings.UseDarkTheme != value )
                                {
                                        settings.UseDarkTheme = value;
                                        _settingsService.SaveCurrentSettings ( );
                                }
                                OnPropertyChanged ( nameof ( IsDarkTheme ) );
                                OnPropertyChanged ( nameof ( CurrentThemeName ) );
                        }
                }

                public string CurrentThemeName => _themeService.CurrentTheme == AppTheme.Dark ? "Dark Mode" : "Light Mode";

                public bool SaveConfigAsDefaultOnClose
                {
                        get => _settingsService.GetCurrentSettings ( ).SaveConfigAsDefaultOnClose;
                        set
                        {
                                var settings = _settingsService.GetCurrentSettings ( );
                                if ( settings.SaveConfigAsDefaultOnClose == value )
                                        return;

                                settings.SaveConfigAsDefaultOnClose = value;
                                _settingsService.SaveCurrentSettings ( );
                                OnPropertyChanged ( nameof ( SaveConfigAsDefaultOnClose ) );
                        }
                }

                public string HotkeyCombination
                {
                        get => _hotkeyCombination;
                        set
                        {
                                if ( _hotkeyCombination != value )
                                {
                                        _hotkeyCombination = value;
                                        OnPropertyChanged ( nameof ( HotkeyCombination ) );
                                        if ( ApplyHotkeyCommand is RelayCommandNoParam relay )
                                                relay.RaiseCanExecuteChanged ( );
                                }
                        }
                }

                public bool AutoReadClipboard
                {
                        get => _autoReadClipboard;
                        set
                        {
                                if ( _autoReadClipboard == value )
                                        return;

                                _autoReadClipboard = value;
                                var settings = _settingsService.GetCurrentSettings ( );
                                if ( settings.Hotkey != null && settings.Hotkey.AutoReadClipboard != value )
                                {
                                        settings.Hotkey.AutoReadClipboard = value;
                                        _settingsService.SaveCurrentSettings ( );
                                }

                                _clipboardManager.SetAutoReadClipboard ( value );
                                OnPropertyChanged ( nameof ( AutoReadClipboard ) );
                        }
                }

                public string Voice
                {
                        get => _settingsService.GetCurrentSettings ( ).Voice;
                        set
                        {
                                if ( string.IsNullOrWhiteSpace ( value ) || !AvailableVoices.Contains ( value ) )
                                        throw new ArgumentException ( $"Invalid voice: {value}" );

                                var settings = _settingsService.GetCurrentSettings ( );
                                if ( settings.Voice != value )
                                {
                                        settings.Voice = value;
                                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
                                        OnPropertyChanged ( nameof ( Voice ) );
                                }
                        }
                }

                public double VoiceRate
                {
                        get => _settingsService.GetCurrentSettings ( ).VoiceRate;
                        set
                        {
                                var settings = _settingsService.GetCurrentSettings ( );
                                if ( settings.VoiceRate != value )
                                {
                                        settings.VoiceRate = value;
                                        _ttsService.SetTTSParameters ( settings.Voice, value, settings.Volume );
                                        OnPropertyChanged ( nameof ( VoiceRate ) );
                                }
                        }
                }

                public int Volume
                {
                        get => _settingsService.GetCurrentSettings ( ).Volume;
                        set
                        {
                                if ( value < 0 || value > 100 )
                                        throw new ArgumentOutOfRangeException ( nameof ( Volume ), "Volume must be between 0 and 100." );

                                var settings = _settingsService.GetCurrentSettings ( );
                                if ( settings.Volume != value )
                                {
                                        settings.Volume = value;
                                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, value );
                                        OnPropertyChanged ( nameof ( Volume ) );
                                }
                        }
                }

                public void OnWindowClosing ( )
                {
                        _statusAnnouncementService.StatusAnnounced -= OnStatusAnnounced;
                        _ttsService.SpeechCompleted -= OnSpeechCompleted;
                        SetPreviewState ( false, null );
                        _ttsService.Stop ( );

                        var settings = _settingsService.GetCurrentSettings ( );
                        if ( settings.SaveConfigAsDefaultOnClose )
                        {
                                _settingsService.SaveCurrentSettingsAsDefault ( );
                        }
                }

                private bool CanApplyHotkey ( )
                {
                        if ( string.IsNullOrWhiteSpace ( _hotkeyCombination ) || _hotkeyCombination == _lastAppliedHotkeyCombination )
                                return false;

                        return TryParseHotkeyCombination ( _hotkeyCombination, out _, out _ );
                }

                private void ApplyHotkey ( )
                {
                        try
                        {
                                UpdateHotkey ( _hotkeyCombination );
                                _lastAppliedHotkeyCombination = _hotkeyCombination;
                                if ( ApplyHotkeyCommand is RelayCommandNoParam relay )
                                        relay.RaiseCanExecuteChanged ( );
                                _settingsService.SaveCurrentSettings ( );
                                PublishStatus ( "StatusMessageHotkeyUpdated", "Hotkey updated.", StatusSeverity.Success );
                        }
                        catch ( Exception ex )
                        {
                                var errorMessage = $"Failed to register hotkey: {_hotkeyCombination}. It might already be in use by another application.";
                                MessageBox.Show ( errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
                                Logger.Warn ( ex, errorMessage );
                                PublishStatus ( "StatusMessageHotkeyRegistrationFailed", "Couldn't register the selected hotkey.", StatusSeverity.Error );
                        }
                }

                private void ExportConfiguration ( )
                {
                        var dialog = new SaveFileDialog
                        {
                                Filter = ConfigurationFileFilter,
                                FileName = DefaultExportFileName,
                                AddExtension = true,
                                DefaultExt = "json"
                        };

                        if ( dialog.ShowDialog ( ) == true && _settingsService.ExportSettings ( dialog.FileName ) )
                        {
                                _messageService.DissonanceMessageBoxShowInfo ( MessageBoxTitles.SettingsServiceInfo, "Configuration exported successfully." );
                                PublishStatus ( "StatusMessageConfigurationExported", "Configuration exported successfully.", StatusSeverity.Success );
                        }
                }

                private void ImportConfiguration ( )
                {
                        var dialog = new OpenFileDialog
                        {
                                Filter = ConfigurationFileFilter,
                                DefaultExt = "json"
                        };

                        if ( dialog.ShowDialog ( ) == true && _settingsService.ImportSettings ( dialog.FileName ) )
                        {
                                ReloadSettingsFromService ( true );
                                _messageService.DissonanceMessageBoxShowInfo ( MessageBoxTitles.SettingsServiceInfo, "Configuration imported successfully." );
                                PublishStatus ( "StatusMessageConfigurationImported", "Configuration imported successfully.", StatusSeverity.Success );
                        }
                }

                private void PreviewVoice ( )
                {
                        var wasPreviewing = IsPreviewing;
                        _ttsService.Stop ( );

                        if ( wasPreviewing )
                        {
                                SetPreviewState ( false, null );
                                return;
                        }

                        if ( string.IsNullOrWhiteSpace ( _previewSampleText ) )
                                return;

                        var settings = _settingsService.GetCurrentSettings ( );
                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
                        var prompt = _ttsService.Speak ( _previewSampleText );
                        SetPreviewState ( prompt != null, prompt );
                }

                private void OnSpeechCompleted ( object? sender, SpeakCompletedEventArgs e )
                {
                        if ( _activePreviewPrompt == null )
                                return;

                        if ( !ReferenceEquals ( e.Prompt, _activePreviewPrompt ) )
                                return;

                        _dispatcher.BeginInvoke ( new Action ( ( ) => SetPreviewState ( false, null ) ) );
                }

                private void SetPreviewState ( bool isPreviewing, Prompt? prompt )
                {
                        if ( isPreviewing && prompt == null )
                                isPreviewing = false;

                        _activePreviewPrompt = isPreviewing ? prompt : null;
                        IsPreviewing = isPreviewing;

                        if ( PreviewVoiceCommand is RelayCommandNoParam previewCommand )
                                previewCommand.RaiseCanExecuteChanged ( );
                }

                private static string GetResourceString ( string key, string fallback )
                {
                        if ( Application.Current?.TryFindResource ( key ) is string value && !string.IsNullOrWhiteSpace ( value ) )
                                return value;

                        return fallback;
                }

                private void ReloadSettingsFromService ( bool reapplyHotkey )
                {
                        var settings = _settingsService.GetCurrentSettings ( );
                        if ( !string.IsNullOrWhiteSpace ( settings.Voice ) && !AvailableVoices.Contains ( settings.Voice ) && AvailableVoices.Any ( ) )
                        {
                                var fallbackVoice = AvailableVoices.First ( );
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.SettingsServiceWarning, $"The voice \"{settings.Voice}\" is not installed. Using \"{fallbackVoice}\" instead." );
                                settings.Voice = fallbackVoice;
                                _settingsService.SaveCurrentSettings ( );
                                PublishStatus ( "StatusMessageVoiceUnavailable", "The saved voice is no longer installed. A fallback voice will be used.", StatusSeverity.Warning );
                        }

                        _themeService.ApplyTheme ( settings.UseDarkTheme ? AppTheme.Dark : AppTheme.Light );
                        if ( _isDarkTheme != settings.UseDarkTheme )
                        {
                                _isDarkTheme = settings.UseDarkTheme;
                                OnPropertyChanged ( nameof ( IsDarkTheme ) );
                        }
                        OnPropertyChanged ( nameof ( CurrentThemeName ) );

                        _hotkeyCombination = ComposeHotkeyString ( settings.Hotkey );
                        OnPropertyChanged ( nameof ( HotkeyCombination ) );
                        OnPropertyChanged ( nameof ( Voice ) );
                        OnPropertyChanged ( nameof ( VoiceRate ) );
                        OnPropertyChanged ( nameof ( Volume ) );
                        OnPropertyChanged ( nameof ( SaveConfigAsDefaultOnClose ) );
                        var autoReadClipboard = settings.Hotkey.AutoReadClipboard;
                        var autoReadChanged = _autoReadClipboard != autoReadClipboard;
                        _autoReadClipboard = autoReadClipboard;
                        if ( autoReadChanged )
                                OnPropertyChanged ( nameof ( AutoReadClipboard ) );
                        _clipboardManager.SetAutoReadClipboard ( _autoReadClipboard );

                        _isDarkTheme = settings.UseDarkTheme;
                        _themeService.ApplyTheme ( _isDarkTheme ? AppTheme.Dark : AppTheme.Light );
                        OnPropertyChanged ( nameof ( IsDarkTheme ) );
                        OnPropertyChanged ( nameof ( CurrentThemeName ) );

                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );

                        if ( reapplyHotkey )
                        {
                                try
                                {
                                        UpdateHotkey ( _hotkeyCombination );
                                        _lastAppliedHotkeyCombination = _hotkeyCombination;
                                }
                                catch ( Exception ex )
                                {
                                        var warning = $"Failed to register hotkey: {_hotkeyCombination}. It might already be in use by another application.";
                                        _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.HotkeyServiceWarning, warning );
                                        Logger.Warn ( ex, warning );
                                        PublishStatus ( "StatusMessageHotkeyRegistrationFailed", "Couldn't register the selected hotkey.", StatusSeverity.Error );
                                }
                        }
                        else
                        {
                                _lastAppliedHotkeyCombination = _hotkeyCombination;
                        }

                        if ( ApplyHotkeyCommand is RelayCommandNoParam relay )
                        {
                                relay.RaiseCanExecuteChanged ( );
                        }
                }

                private void SaveCurrentConfigurationAsDefault ( )
                {
                        if ( _settingsService.SaveCurrentSettingsAsDefault ( ) )
                        {
                                ReloadSettingsFromService ( false );
                                _messageService.DissonanceMessageBoxShowInfo ( MessageBoxTitles.SettingsServiceInfo, "Configuration saved as default.", TimeSpan.FromSeconds ( 20 ) );
                                PublishStatus ( "StatusMessageConfigurationSavedAsDefault", "Configuration saved as default.", StatusSeverity.Success );
                        }
                }

                private void UpdateHotkey ( string hotkeyCombination )
                {
                        if ( !TryParseHotkeyCombination ( hotkeyCombination, out var modifiers, out Key newKey ) )
                        {
                                throw new ArgumentException ( "Hotkey combination must include at least one modifier and a key.", nameof ( hotkeyCombination ) );
                        }

                        var normalizedModifiers = string.Join ( "+", modifiers );
                        var keyText = newKey.ToString ( );
                        var canonicalCombination = $"{normalizedModifiers}+{keyText}";

                        if ( !string.Equals ( _hotkeyCombination, canonicalCombination, StringComparison.Ordinal ) )
                        {
                                _hotkeyCombination = canonicalCombination;
                                OnPropertyChanged ( nameof ( HotkeyCombination ) );
                        }

                        var settings = _settingsService.GetCurrentSettings ( );
                        var newHotkey = new AppSettings.HotkeySettings
                        {
                                Modifiers = normalizedModifiers,
                                Key = keyText
                        };

                        var hotkeyValuesDiffer = settings.Hotkey.Modifiers != newHotkey.Modifiers || settings.Hotkey.Key != newHotkey.Key;

                        if ( hotkeyValuesDiffer )
                        {
                                settings.Hotkey = newHotkey;
                        }

                        if ( string.Equals ( _lastAppliedHotkeyCombination, canonicalCombination, StringComparison.OrdinalIgnoreCase ) )
                        {
                                return;
                        }

                        _hotkeyService.RegisterHotkey ( newHotkey );

                        if ( hotkeyValuesDiffer )
                        {
                                _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
                        }
                }

                private static bool TryParseHotkeyCombination ( string combination, out string[] modifiers, out Key key )
                {
                        modifiers = Array.Empty<string> ( );
                        key = Key.None;

                        if ( string.IsNullOrWhiteSpace ( combination ) )
                                return false;

                        var parts = combination.Split ( new[] { '+' }, StringSplitOptions.RemoveEmptyEntries );
                        if ( parts.Length < 2 )
                                return false;

                        var modifierCandidates = parts.Take ( parts.Length - 1 )
                                                        .Select ( part => part.Trim ( ) )
                                                        .Where ( part => !string.IsNullOrWhiteSpace ( part ) )
                                                        .ToArray ( );

                        if ( modifierCandidates.Length == 0 )
                                return false;

                        string[] modifierParts;
                        try
                        {
                                modifierParts = modifierCandidates.Select ( NormalizeModifierName ).ToArray ( );
                        }
                        catch ( ArgumentException )
                        {
                                return false;
                        }

                        var keyPart = parts[parts.Length - 1].Trim ( );
                        if ( !Enum.TryParse ( keyPart, true, out key ) )
                                return false;

                        modifiers = modifierParts;
                        return true;
                }

                private static string NormalizeModifierName ( string modifier )
                {
                        if ( string.IsNullOrWhiteSpace ( modifier ) )
                                throw new ArgumentException ( "Modifier cannot be null or whitespace.", nameof ( modifier ) );

                        switch ( modifier.Trim ( ).ToLowerInvariant ( ) )
                        {
                                case "ctrl":
                                case "control":
                                        return "Ctrl";
                                case "shift":
                                        return "Shift";
                                case "alt":
                                        return "Alt";
                                case "win":
                                case "windows":
                                case "cmd":
                                case "command":
                                case "meta":
                                        return "Win";
                                default:
                                        throw new ArgumentException ( $"Unsupported modifier: {modifier}", nameof ( modifier ) );
                        }
                }

                private static string ComposeHotkeyString ( AppSettings.HotkeySettings? hotkey )
                {
                        if ( hotkey == null || string.IsNullOrWhiteSpace ( hotkey.Key ) )
                                return string.Empty;

                        var key = hotkey.Key.Trim ( );
                        if ( string.IsNullOrWhiteSpace ( hotkey.Modifiers ) )
                                return key;

                        var candidate = $"{hotkey.Modifiers.Trim ( )}+{key}";
                        return TryParseHotkeyCombination ( candidate, out var modifiers, out var parsedKey )
                                ? string.Join ( "+", modifiers ) + "+" + parsedKey.ToString ( )
                                : candidate;
                }

                private void OnStatusAnnounced ( object? sender, StatusAnnouncement announcement )
                {
                        if ( announcement == null )
                                return;

                        if ( _dispatcher.CheckAccess ( ) )
                        {
                                AppendStatus ( announcement );
                        }
                        else
                        {
                                _dispatcher.BeginInvoke ( new Action ( ( ) => AppendStatus ( announcement ) ) );
                        }
                }

                private void AppendStatus ( StatusAnnouncement announcement )
                {
                        _statusHistory.Add ( announcement );
                        TrimStatusHistory ( );
                        _latestStatus = announcement;
                        OnPropertyChanged ( nameof ( StatusMessage ) );
                        OnPropertyChanged ( nameof ( StatusSeverity ) );
                        OnPropertyChanged ( nameof ( IsStatusMessageVisible ) );
                }

                private void TrimStatusHistory ( )
                {
                        while ( _statusHistory.Count > MaxStatusItems )
                        {
                                _statusHistory.RemoveAt ( 0 );
                        }
                }

                private void PublishStatus ( string resourceKey, string fallbackMessage, StatusSeverity severity )
                {
                        _statusAnnouncementService.AnnounceFromResource ( resourceKey, fallbackMessage, severity );
                }

                private void NavigateToSection ( object parameter )
                {
                        if ( parameter is NavigationSectionViewModel section )
                        {
                                if ( SelectedSection != section )
                                        SelectedSection = section;
                        }
                        else
                        {
                                if ( SelectedSection != null )
                                        SelectedSection = null;
                        }

                        if ( IsNavigationMenuOpen )
                                IsNavigationMenuOpen = false;
                }

                protected void OnPropertyChanged ( string propertyName )
                {
                        PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
                }
        }
}
