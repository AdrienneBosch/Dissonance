using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

using Dissonance.Infrastructure.Commands;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;

namespace Dissonance.ViewModels
{
        public class ManualSpeechViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
        {
                private readonly ITTSService _ttsService;
                private readonly ISettingsService _settingsService;
                private readonly RelayCommand _speakInputCommand;
                private readonly RelayCommandNoParam _clearInputCommand;
                private readonly Dictionary<string, List<string>> _propertyErrors = new Dictionary<string, List<string>> ( StringComparer.Ordinal );
                private string _inputText;

                public ManualSpeechViewModel ( ITTSService ttsService, ISettingsService settingsService )
                {
                        _ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );
                        _settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );

                        var settings = _settingsService.GetCurrentSettings ( );
                        _inputText = settings?.ManualSpeechLastInput ?? string.Empty;
                        ValidateInput ( );

                        _speakInputCommand = new RelayCommand ( _ => SpeakInputInternal ( ), _ => CanSpeak );
                        _clearInputCommand = new RelayCommandNoParam ( ClearInput, ( ) => !string.IsNullOrWhiteSpace ( InputText ) );
                }

                public event PropertyChangedEventHandler? PropertyChanged;

                public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

                public string InputText
                {
                        get => _inputText;
                        set
                        {
                                if ( _inputText == value )
                                        return;

                                _inputText = value ?? string.Empty;
                                OnPropertyChanged ( nameof ( InputText ) );
                                ValidateInput ( );
                                _speakInputCommand.RaiseCanExecuteChanged ( );
                                _clearInputCommand.RaiseCanExecuteChanged ( );
                                OnPropertyChanged ( nameof ( CanSpeak ) );
                                OnPropertyChanged ( nameof ( InputValidationMessage ) );
                        }
                }

                public bool CanSpeak => !string.IsNullOrWhiteSpace ( InputText );

                public string InputValidationMessage
                {
                        get
                        {
                                if ( _propertyErrors.TryGetValue ( nameof ( InputText ), out var errors ) && errors.Any ( ) )
                                        return errors[0];

                                return string.Empty;
                        }
                }

                public ICommand SpeakInputCommand => _speakInputCommand;

                public ICommand ClearInputCommand => _clearInputCommand;

                public bool HasErrors => _propertyErrors.Count > 0;

                public IEnumerable GetErrors ( string? propertyName )
                {
                        if ( string.IsNullOrWhiteSpace ( propertyName ) )
                                return _propertyErrors.SelectMany ( kvp => kvp.Value );

                        if ( _propertyErrors.TryGetValue ( propertyName, out var errors ) )
                                return errors;

                        return Array.Empty<string> ( );
                }

                private void SpeakInputInternal ( )
                {
                        var textToSpeak = InputText?.Trim ( );
                        if ( string.IsNullOrWhiteSpace ( textToSpeak ) )
                                return;

                        _ttsService.Stop ( );
                        _ttsService.Speak ( textToSpeak );
                        PersistInput ( textToSpeak );
                }

                private void ClearInput ( )
                {
                        if ( string.IsNullOrEmpty ( InputText ) )
                                return;

                        InputText = string.Empty;
                        PersistInput ( string.Empty );
                }

                private void PersistInput ( string text )
                {
                        var settings = _settingsService.GetCurrentSettings ( );
                        if ( settings == null )
                                return;

                        if ( string.Equals ( settings.ManualSpeechLastInput, text, StringComparison.Ordinal ) )
                                return;

                        settings.ManualSpeechLastInput = text;
                        _settingsService.SaveCurrentSettings ( );
                }

                private void ValidateInput ( )
                {
                        var errors = new List<string> ( );
                        if ( string.IsNullOrWhiteSpace ( InputText ) )
                        {
                                errors.Add ( "Enter text to speak." );
                        }

                        UpdateErrors ( nameof ( InputText ), errors );
                }

                private void UpdateErrors ( string propertyName, List<string> errors )
                {
                        var hasExistingErrors = _propertyErrors.TryGetValue ( propertyName, out var existingErrors );
                        var existingErrorsList = hasExistingErrors && existingErrors != null ? existingErrors : Array.Empty<string> ( );
                        var hasNewErrors = errors.Count > 0;

                        if ( !hasNewErrors )
                        {
                                if ( hasExistingErrors )
                                        _propertyErrors.Remove ( propertyName );
                        }
                        else
                        {
                                _propertyErrors[propertyName] = errors;
                        }

                        if ( hasExistingErrors != hasNewErrors || ( hasNewErrors && !existingErrorsList.SequenceEqual ( errors ) ) )
                        {
                                ErrorsChanged?.Invoke ( this, new DataErrorsChangedEventArgs ( propertyName ) );
                                OnPropertyChanged ( nameof ( InputValidationMessage ) );
                                OnPropertyChanged ( nameof ( HasErrors ) );
                        }
                }

                private void OnPropertyChanged ( string propertyName )
                {
                        PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
                }
        }
}
