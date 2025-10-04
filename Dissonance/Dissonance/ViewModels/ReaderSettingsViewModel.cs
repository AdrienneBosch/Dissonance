using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Dissonance.ViewModels
{
        public class ReaderSettingsViewModel : INotifyPropertyChanged, IDisposable
        {
                private readonly MainWindowViewModel _mainViewModel;
                private bool _isDisposed;

                public ReaderSettingsViewModel ( MainWindowViewModel mainViewModel )
                {
                        _mainViewModel = mainViewModel ?? throw new ArgumentNullException ( nameof ( mainViewModel ) );
                        _mainViewModel.PropertyChanged += OnParentPropertyChanged;
                }

                public ObservableCollection<string> AvailableVoices => _mainViewModel.AvailableVoices;

                public string Voice
                {
                        get => _mainViewModel.Voice;
                        set => _mainViewModel.Voice = value;
                }

                public double VoiceRate
                {
                        get => _mainViewModel.VoiceRate;
                        set => _mainViewModel.VoiceRate = value;
                }

                public int Volume
                {
                        get => _mainViewModel.Volume;
                        set => _mainViewModel.Volume = value;
                }

                public ICommand PreviewVoiceCommand => _mainViewModel.PreviewVoiceCommand;

                public string PreviewVoiceButtonLabel => _mainViewModel.PreviewVoiceButtonLabel;

                public string PreviewVoiceButtonToolTip => _mainViewModel.PreviewVoiceButtonToolTip;

                public string PreviewVoiceHelpText => _mainViewModel.PreviewVoiceHelpText;

                public bool IsPreviewing => _mainViewModel.IsPreviewing;

                public event PropertyChangedEventHandler? PropertyChanged;

                private void OnParentPropertyChanged ( object? sender, PropertyChangedEventArgs e )
                {
                        if ( string.IsNullOrEmpty ( e.PropertyName ) )
                        {
                                OnPropertyChanged ( nameof ( Voice ) );
                                OnPropertyChanged ( nameof ( VoiceRate ) );
                                OnPropertyChanged ( nameof ( Volume ) );
                                OnPropertyChanged ( nameof ( PreviewVoiceButtonLabel ) );
                                OnPropertyChanged ( nameof ( PreviewVoiceButtonToolTip ) );
                                OnPropertyChanged ( nameof ( PreviewVoiceHelpText ) );
                                OnPropertyChanged ( nameof ( IsPreviewing ) );
                                return;
                        }

                        switch ( e.PropertyName )
                        {
                                case nameof ( MainWindowViewModel.Voice ):
                                        OnPropertyChanged ( nameof ( Voice ) );
                                        break;
                                case nameof ( MainWindowViewModel.VoiceRate ):
                                        OnPropertyChanged ( nameof ( VoiceRate ) );
                                        break;
                                case nameof ( MainWindowViewModel.Volume ):
                                        OnPropertyChanged ( nameof ( Volume ) );
                                        break;
                                case nameof ( MainWindowViewModel.IsPreviewing ):
                                        OnPropertyChanged ( nameof ( IsPreviewing ) );
                                        OnPropertyChanged ( nameof ( PreviewVoiceButtonLabel ) );
                                        break;
                                case nameof ( MainWindowViewModel.PreviewVoiceButtonLabel ):
                                        OnPropertyChanged ( nameof ( PreviewVoiceButtonLabel ) );
                                        break;
                                case nameof ( MainWindowViewModel.PreviewVoiceButtonToolTip ):
                                        OnPropertyChanged ( nameof ( PreviewVoiceButtonToolTip ) );
                                        break;
                                case nameof ( MainWindowViewModel.PreviewVoiceHelpText ):
                                        OnPropertyChanged ( nameof ( PreviewVoiceHelpText ) );
                                        break;
                        }
                }

                protected virtual void OnPropertyChanged ( string propertyName )
                {
                        PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
                }

                public void Dispose ( )
                {
                        if ( _isDisposed )
                                return;

                        _mainViewModel.PropertyChanged -= OnParentPropertyChanged;
                        _isDisposed = true;
                }
        }
}
