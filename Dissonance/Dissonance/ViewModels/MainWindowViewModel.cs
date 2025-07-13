using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Dissonance.Services.HotkeyService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;
using Dissonance.Infrastructure.Commands;
using NLog;

namespace Dissonance.ViewModels
{
	public interface IMagnifierService
	{
		double GetCurrentZoom();
		void ToggleZoom();
		event EventHandler ZoomChanged;
	}

	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private readonly IHotkeyService _hotkeyService;
		private readonly ISettingsService _settingsService;
		private readonly ITTSService _ttsService;
		private readonly IMagnifierService _magnifierService;
		private string _hotkeyCombination;
		private string _lastAppliedHotkeyCombination;
		private double _zoomLevel;
		public ICommand ApplyHotkeyCommand { get; }

		public MainWindowViewModel ( ISettingsService settingsService, ITTSService ttsService, IHotkeyService hotkeyService, IMagnifierService magnifierService )
		{
			_settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
			_ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );
			_hotkeyService = hotkeyService ?? throw new ArgumentNullException ( nameof ( hotkeyService ) );
			_magnifierService = magnifierService ?? throw new ArgumentNullException ( nameof ( magnifierService ) );

			var installedVoices = new System.Speech.Synthesis.SpeechSynthesizer().GetInstalledVoices();
			foreach ( var voice in installedVoices )
			{
				AvailableVoices.Add ( voice.VoiceInfo.Name );
			}

			var settings = _settingsService.GetCurrentSettings();
			_hotkeyCombination = settings.Hotkey.Modifiers + "+" + settings.Hotkey.Key;
			_lastAppliedHotkeyCombination = _hotkeyCombination;
			UpdateHotkey(_hotkeyCombination);
			ApplyHotkeyCommand = new RelayCommandNoParam(ApplyHotkey, CanApplyHotkey);

			// Magnifier integration
			_zoomLevel = _magnifierService.GetCurrentZoom();
			_magnifierService.ZoomChanged += (s, e) =>
			{
				ZoomLevel = _magnifierService.GetCurrentZoom();
			};

			// Hotkey trigger handling for magnifier
			_hotkeyService.HotkeyPressed += OnHotkeyPressed;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public ObservableCollection<string> AvailableVoices { get; } = new ObservableCollection<string> ( );

		public string HotkeyCombination
		{
			get => _hotkeyCombination;
			set
			{
				if ( _hotkeyCombination != value )
				{
					_hotkeyCombination = value;
					OnPropertyChanged ( nameof ( HotkeyCombination ) );
					if (ApplyHotkeyCommand is Dissonance.Infrastructure.Commands.RelayCommandNoParam relay)
						relay.RaiseCanExecuteChanged();
				}
			}
		}

		public string Voice
		{
			get => _settingsService.GetCurrentSettings ( ).Voice;
			set
			{
				if ( string.IsNullOrWhiteSpace ( value ) || !AvailableVoices.Contains ( value ) ) // Validate voice
					throw new ArgumentException ( $"Invalid voice: {value}" );

				var settings = _settingsService.GetCurrentSettings();
				if ( settings.Voice != value )
				{
					settings.Voice = value;
					_ttsService.SetTTSParameters ( value, settings.VoiceRate, settings.Volume ); // Live update
					OnPropertyChanged ( nameof ( Voice ) );
				}
			}
		}

		public double VoiceRate
		{
			get => _settingsService.GetCurrentSettings ( ).VoiceRate;
			set
			{
				var settings = _settingsService.GetCurrentSettings();
				if ( settings.VoiceRate != value )
				{
					settings.VoiceRate = value;
					_ttsService.SetTTSParameters ( settings.Voice, value, settings.Volume ); // Live update
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

				var settings = _settingsService.GetCurrentSettings();
				if ( settings.Volume != value )
				{
					settings.Volume = value;
					_ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, value );
					OnPropertyChanged ( nameof ( Volume ) );
				}
			}
		}

		public double ZoomLevel
		{
			get => _zoomLevel;
			set
			{
				if (_zoomLevel != value)
				{
					_zoomLevel = value;
					OnPropertyChanged(nameof(ZoomLevel));
					OnPropertyChanged(nameof(ZoomLevelDisplay));
				}
			}
		}

		public string ZoomLevelDisplay => $"Zoom: {ZoomLevel * 100:0}%";

		private bool CanApplyHotkey()
		{
			if (string.IsNullOrWhiteSpace(_hotkeyCombination) || _hotkeyCombination == _lastAppliedHotkeyCombination)
				return false;
			var parts = _hotkeyCombination.Split('+');
			if (parts.Length < 2)
				return false;
			var key = parts.Last();
			return Enum.TryParse(key, true, out System.Windows.Input.Key _);
		}

		private void ApplyHotkey()
		{
			try
			{
				UpdateHotkey(_hotkeyCombination);
				_lastAppliedHotkeyCombination = _hotkeyCombination;
				if (ApplyHotkeyCommand is Dissonance.Infrastructure.Commands.RelayCommandNoParam relay)
					relay.RaiseCanExecuteChanged();
			}
			catch (Exception ex)
			{
				var errorMessage = $"Failed to register hotkey: {_hotkeyCombination}. It might already be in use by another application.";
				MessageBox.Show(errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Logger.Warn(ex, errorMessage);
			}
		}

		private void UpdateHotkey ( string hotkeyCombination )
		{ 
			if ( string.IsNullOrWhiteSpace ( hotkeyCombination ) )
			{
				throw new ArgumentException ( "Hotkey combination cannot be null, empty, or whitespace." );
			}
 
			var parts = hotkeyCombination.Split('+');
			if ( parts.Length < 2 )
			{
				throw new ArgumentException ( "Hotkey combination must include at least one modifier and a key." );
			}

			var modifiers = string.Join("+", parts.Take(parts.Length - 1)); // Combine all except the last part as modifiers
			var key = parts.Last();

			if ( !Enum.TryParse ( key, true, out Key newKey ) )
			{
				throw new ArgumentException ( $"Invalid key value: {key}" );
			}

			var settings = _settingsService.GetCurrentSettings();
			var newHotkey = new AppSettings.HotkeySettings
			{
				Modifiers = modifiers,
				Key = newKey.ToString()
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

				var ttsSettings = _settingsService.GetCurrentSettings();
				_ttsService.SetTTSParameters ( ttsSettings.Voice, ttsSettings.VoiceRate, ttsSettings.Volume );
			}
		}

		private void OnHotkeyPressed()
		{
			_magnifierService.ToggleZoom();
			ZoomLevel = _magnifierService.GetCurrentZoom();
		}

		protected void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
		}
	}
}