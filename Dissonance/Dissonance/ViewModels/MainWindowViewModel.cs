using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Dissonance.Infrastructure.Commands;
using Dissonance.Infrastructure.Constants;
using Dissonance.Services.HotkeyService;
using Dissonance.Services.MessageService;
using Dissonance.Services.SettingsService;
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
                private bool _isDarkTheme;
                private string _hotkeyCombination;
                private string _lastAppliedHotkeyCombination;

                public MainWindowViewModel ( ISettingsService settingsService, ITTSService ttsService, IHotkeyService hotkeyService, IThemeService themeService, IMessageService messageService )
                {
                        _settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
                        _ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );
                        _hotkeyService = hotkeyService ?? throw new ArgumentNullException ( nameof ( hotkeyService ) );
                        _themeService = themeService ?? throw new ArgumentNullException ( nameof ( themeService ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );

                        SaveSettingsCommand = new RelayCommandNoParam ( SaveCurrentConfiguration );
                        SaveDefaultSettingsCommand = new RelayCommandNoParam ( SaveCurrentConfigurationAsDefault );
                        ExportSettingsCommand = new RelayCommandNoParam ( ExportConfiguration );
                        ImportSettingsCommand = new RelayCommandNoParam ( ImportConfiguration );
                        ApplyHotkeyCommand = new RelayCommandNoParam ( ApplyHotkey, CanApplyHotkey );

                        var installedVoices = new System.Speech.Synthesis.SpeechSynthesizer ( ).GetInstalledVoices ( );
                        foreach ( var voice in installedVoices )
                        {
                                AvailableVoices.Add ( voice.VoiceInfo.Name );
                        }

                        var settings = _settingsService.GetCurrentSettings ( );
                        _hotkeyCombination = settings.Hotkey.Modifiers + "+" + settings.Hotkey.Key;
                        _lastAppliedHotkeyCombination = _hotkeyCombination;
                        UpdateHotkey ( _hotkeyCombination );
                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );

                        _isDarkTheme = _themeService.CurrentTheme == AppTheme.Dark;
                        OnPropertyChanged ( nameof ( IsDarkTheme ) );
                        OnPropertyChanged ( nameof ( CurrentThemeName ) );
                        OnPropertyChanged ( nameof ( SaveConfigAsDefaultOnClose ) );
                }

                public event PropertyChangedEventHandler PropertyChanged;

                public ObservableCollection<string> AvailableVoices { get; } = new ObservableCollection<string> ( );

                public ICommand ApplyHotkeyCommand { get; }

                public ICommand ExportSettingsCommand { get; }

                public ICommand ImportSettingsCommand { get; }

                public ICommand SaveDefaultSettingsCommand { get; }

                public ICommand SaveSettingsCommand { get; }

                public bool IsDarkTheme
                {
                        get => _isDarkTheme;
                        set
                        {
                                if ( _isDarkTheme == value )
                                        return;

                                _isDarkTheme = value;
                                _themeService.ApplyTheme ( value ? AppTheme.Dark : AppTheme.Light );
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
                        _settingsService.SaveCurrentSettings ( );
                        if ( _settingsService.GetCurrentSettings ( ).SaveConfigAsDefaultOnClose )
                        {
                                _settingsService.SaveCurrentSettingsAsDefault ( );
                        }
                }

                private bool CanApplyHotkey ( )
                {
                        if ( string.IsNullOrWhiteSpace ( _hotkeyCombination ) || _hotkeyCombination == _lastAppliedHotkeyCombination )
                                return false;

                        var parts = _hotkeyCombination.Split ( '+' );
                        if ( parts.Length < 2 )
                                return false;

                        var key = parts.Last ( );
                        return Enum.TryParse ( key, true, out System.Windows.Input.Key _ );
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
                        }
                        catch ( Exception ex )
                        {
                                var errorMessage = $"Failed to register hotkey: {_hotkeyCombination}. It might already be in use by another application.";
                                MessageBox.Show ( errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
                                Logger.Warn ( ex, errorMessage );
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
                        }
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
                        }

                        _hotkeyCombination = settings.Hotkey.Modifiers + "+" + settings.Hotkey.Key;
                        _lastAppliedHotkeyCombination = _hotkeyCombination;
                        OnPropertyChanged ( nameof ( HotkeyCombination ) );
                        OnPropertyChanged ( nameof ( Voice ) );
                        OnPropertyChanged ( nameof ( VoiceRate ) );
                        OnPropertyChanged ( nameof ( Volume ) );
                        OnPropertyChanged ( nameof ( SaveConfigAsDefaultOnClose ) );

                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );

                        if ( reapplyHotkey )
                        {
                                try
                                {
                                        UpdateHotkey ( _hotkeyCombination );
                                }
                                catch ( Exception ex )
                                {
                                        var warning = $"Failed to register hotkey: {_hotkeyCombination}. It might already be in use by another application.";
                                        _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.HotkeyServiceWarning, warning );
                                        Logger.Warn ( ex, warning );
                                }
                        }

                        if ( ApplyHotkeyCommand is RelayCommandNoParam relay )
                        {
                                relay.RaiseCanExecuteChanged ( );
                        }
                }

                private void SaveCurrentConfiguration ( )
                {
                        if ( _settingsService.SaveCurrentSettings ( ) )
                        {
                                ReloadSettingsFromService ( false );
                                _messageService.DissonanceMessageBoxShowInfo ( MessageBoxTitles.SettingsServiceInfo, "Configuration saved." );
                        }
                }

                private void SaveCurrentConfigurationAsDefault ( )
                {
                        if ( _settingsService.SaveCurrentSettingsAsDefault ( ) )
                        {
                                ReloadSettingsFromService ( false );
                                _messageService.DissonanceMessageBoxShowInfo ( MessageBoxTitles.SettingsServiceInfo, "Configuration saved as default." );
                        }
                }

                private void UpdateHotkey ( string hotkeyCombination )
                {
                        if ( string.IsNullOrWhiteSpace ( hotkeyCombination ) )
                        {
                                throw new ArgumentException ( "Hotkey combination cannot be null, empty, or whitespace." );
                        }

                        var parts = hotkeyCombination.Split ( '+' );
                        if ( parts.Length < 2 )
                        {
                                throw new ArgumentException ( "Hotkey combination must include at least one modifier and a key." );
                        }

                        var modifiers = string.Join ( "+", parts.Take ( parts.Length - 1 ) );
                        var key = parts.Last ( );

                        if ( !Enum.TryParse ( key, true, out Key newKey ) )
                        {
                                throw new ArgumentException ( $"Invalid key value: {key}" );
                        }

                        var settings = _settingsService.GetCurrentSettings ( );
                        var newHotkey = new AppSettings.HotkeySettings
                        {
                                Modifiers = modifiers,
                                Key = newKey.ToString ( )
                        };

                        if ( settings.Hotkey.Modifiers != newHotkey.Modifiers || settings.Hotkey.Key != newHotkey.Key )
                        {
                                try
                                {
                                        _hotkeyService.RegisterHotkey ( newHotkey );
                                        settings.Hotkey = newHotkey;
                                        OnPropertyChanged ( nameof ( HotkeyCombination ) );
                                }
                                catch ( Exception ex )
                                {
                                        var errorMessage = $"Failed to register hotkey: {hotkeyCombination}. It might already be in use by another application.";
                                        MessageBox.Show ( errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
                                        Logger.Warn ( errorMessage, ex );
                                }

                                var ttsSettings = _settingsService.GetCurrentSettings ( );
                                _ttsService.SetTTSParameters ( ttsSettings.Voice, ttsSettings.VoiceRate, ttsSettings.Volume );
                        }
                }

                protected void OnPropertyChanged ( string propertyName )
                {
                        PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
                }
        }
}
