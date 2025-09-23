using System;
using System.IO;

using Dissonance.Infrastructure.Constants;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using static Dissonance.AppSettings;

namespace Dissonance.Services.SettingsService
{
        internal class SettingsService : ISettingsService
        {
                private const string SettingsFilePath = "appsettings.json";
                private const string DefaultSettingsFilePath = "appsettings.default.json";
                private readonly ILogger<SettingsService> _logger;
                private readonly Dissonance.Services.MessageService.IMessageService _messageService;
                private AppSettings _currentSettings;
                private AppSettings _defaultSettings;

                public SettingsService ( ILogger<SettingsService> logger, Dissonance.Services.MessageService.IMessageService messageService )
                {
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );

                        _defaultSettings = LoadDefaultSettings ( );
                        _currentSettings = LoadSettings ( );
                }

                public AppSettings GetCurrentSettings ( ) => _currentSettings;

                public AppSettings LoadSettings ( )
                {
                        if ( !File.Exists ( SettingsFilePath ) )
                        {
                                _logger.LogInformation ( "Settings file not found. Creating a new one with default values." );
                                _currentSettings = CloneSettings ( _defaultSettings );
                                WriteSettingsToFile ( SettingsFilePath, _currentSettings, "Failed to save settings." );
                                return _currentSettings;
                        }

                        try
                        {
                                var json = File.ReadAllText ( SettingsFilePath );
                                var deserialized = JsonConvert.DeserializeObject<AppSettings> ( json );
                                _currentSettings = NormalizeSettings ( deserialized, _defaultSettings );
                                return _currentSettings;
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, "Failed to load settings, reverting to default.", ex );
                                _currentSettings = CloneSettings ( _defaultSettings );
                                return _currentSettings;
                        }
                }

                public void ResetToFactorySettings ( )
                {
                        _logger.LogInformation ( "Resetting settings to default values." );
                        _currentSettings = CloneSettings ( _defaultSettings );
                        SaveCurrentSettings ( );
                }

                public void SaveSettings ( AppSettings settings )
                {
                        if ( settings == null )
                                throw new ArgumentNullException ( nameof ( settings ) );

                        var sanitized = NormalizeSettings ( settings, _defaultSettings );
                        if ( WriteSettingsToFile ( SettingsFilePath, sanitized, "Failed to save settings." ) )
                        {
                                _currentSettings = sanitized;
                        }
                }

                public bool SaveCurrentSettings ( )
                {
                        _currentSettings = NormalizeSettings ( _currentSettings, _defaultSettings );
                        return WriteSettingsToFile ( SettingsFilePath, _currentSettings, "Failed to save settings." );
                }

                public bool SaveCurrentSettingsAsDefault ( )
                {
                        var sanitized = NormalizeSettings ( _currentSettings, GetBuiltInDefaultSettings ( ) );
                        if ( !WriteSettingsToFile ( DefaultSettingsFilePath, sanitized, "Failed to save default settings." ) )
                                return false;

                        _defaultSettings = CloneSettings ( sanitized );
                        _currentSettings = CloneSettings ( sanitized );
                        return WriteSettingsToFile ( SettingsFilePath, _currentSettings, "Failed to save settings." );
                }

                public bool ExportSettings ( string destinationPath )
                {
                        if ( string.IsNullOrWhiteSpace ( destinationPath ) )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, "A valid export path was not provided." );
                                return false;
                        }

                        var exportSettings = NormalizeSettings ( _currentSettings, _defaultSettings );
                        var success = WriteSettingsToFile ( destinationPath, exportSettings, "Failed to export settings." );
                        if ( success )
                        {
                                _logger.LogInformation ( "Exported settings to {Path}.", destinationPath );
                        }

                        return success;
                }

                public bool ImportSettings ( string sourcePath )
                {
                        if ( string.IsNullOrWhiteSpace ( sourcePath ) || !File.Exists ( sourcePath ) )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, "The selected configuration file could not be found." );
                                return false;
                        }

                        try
                        {
                                var json = File.ReadAllText ( sourcePath );
                                var importedSettings = JsonConvert.DeserializeObject<AppSettings> ( json );
                                _currentSettings = NormalizeSettings ( importedSettings, _defaultSettings );
                                var success = WriteSettingsToFile ( SettingsFilePath, _currentSettings, "Failed to save settings." );
                                if ( success )
                                {
                                        _logger.LogInformation ( "Imported settings from {Path}.", sourcePath );
                                }

                                return success;
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, "Failed to import settings.", ex );
                                return false;
                        }
                }

                private AppSettings LoadDefaultSettings ( )
                {
                        if ( !File.Exists ( DefaultSettingsFilePath ) )
                        {
                                _logger.LogInformation ( "Default settings file not found. Using built-in defaults." );
                                return GetBuiltInDefaultSettings ( );
                        }

                        try
                        {
                                var json = File.ReadAllText ( DefaultSettingsFilePath );
                                var deserialized = JsonConvert.DeserializeObject<AppSettings> ( json );
                                var normalized = NormalizeSettings ( deserialized, GetBuiltInDefaultSettings ( ) );
                                _logger.LogInformation ( "Loaded default settings from file." );
                                return normalized;
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.SettingsServiceWarning, "Failed to load default settings. Reverting to built-in defaults." );
                                _logger.LogWarning ( ex, "Failed to load default settings. Reverting to built-in defaults." );
                                return GetBuiltInDefaultSettings ( );
                        }
                }

                private static AppSettings GetBuiltInDefaultSettings ( )
                {
                        return new AppSettings
                        {
                                VoiceRate = 1.0,
                                Volume = 50,
                                Voice = "Microsoft David",
                                SaveConfigAsDefaultOnClose = false,
                                Hotkey = new HotkeySettings { Modifiers = "Alt", Key = "E" },
                        };
                }

                private static AppSettings CloneSettings ( AppSettings settings )
                {
                        return new AppSettings
                        {
                                Voice = settings.Voice,
                                VoiceRate = settings.VoiceRate,
                                Volume = settings.Volume,
                                SaveConfigAsDefaultOnClose = settings.SaveConfigAsDefaultOnClose,
                                Hotkey = new HotkeySettings
                                {
                                        Modifiers = settings.Hotkey?.Modifiers ?? string.Empty,
                                        Key = settings.Hotkey?.Key ?? string.Empty,
                                }
                        };
                }

                private static AppSettings NormalizeSettings ( AppSettings? settings, AppSettings fallback )
                {
                        var reference = fallback ?? GetBuiltInDefaultSettings ( );
                        if ( settings == null )
                                return CloneSettings ( reference );

                        var normalized = new AppSettings
                        {
                                Voice = string.IsNullOrWhiteSpace ( settings.Voice ) ? reference.Voice : settings.Voice,
                                VoiceRate = settings.VoiceRate <= 0 ? reference.VoiceRate : settings.VoiceRate,
                                Volume = settings.Volume < 0 || settings.Volume > 100 ? reference.Volume : settings.Volume,
                                SaveConfigAsDefaultOnClose = settings.SaveConfigAsDefaultOnClose,
                                Hotkey = new HotkeySettings
                                {
                                        Modifiers = string.IsNullOrWhiteSpace ( settings.Hotkey?.Modifiers ) ? reference.Hotkey.Modifiers : settings.Hotkey.Modifiers,
                                        Key = string.IsNullOrWhiteSpace ( settings.Hotkey?.Key ) ? reference.Hotkey.Key : settings.Hotkey.Key,
                                }
                        };

                        return normalized;
                }

                private bool WriteSettingsToFile ( string path, AppSettings settings, string failureMessage )
                {
                        try
                        {
                                var json = JsonConvert.SerializeObject ( settings, Formatting.Indented );
                                File.WriteAllText ( path, json );
                                return true;
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, failureMessage, ex );
                                return false;
                        }
                }
        }
}
