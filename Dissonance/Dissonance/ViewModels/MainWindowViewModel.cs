using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Dissonance.Services.HotkeyService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;

using NLog;

namespace Dissonance.ViewModels
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private readonly ISettingsService _settingsService;
		public event PropertyChangedEventHandler PropertyChanged;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public ObservableCollection<string> AvailableVoices { get; } = new ObservableCollection<string> ( );
		private string _hotkeyCombination;

		private readonly ITTSService _ttsService;

		private readonly IHotkeyService _hotkeyService;

		public MainWindowViewModel ( ISettingsService settingsService, ITTSService ttsService, IHotkeyService hotkeyService )
		{
			_settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
			_ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );
			_hotkeyService = hotkeyService ?? throw new ArgumentNullException ( nameof ( hotkeyService ) );

			var installedVoices = new System.Speech.Synthesis.SpeechSynthesizer().GetInstalledVoices();
			foreach ( var voice in installedVoices )
			{
				AvailableVoices.Add ( voice.VoiceInfo.Name );
			}

			var settings = _settingsService.GetCurrentSettings();
			HotkeyCombination = settings.Hotkey.Modifiers + "+" + settings.Hotkey.Key;
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
					_settingsService.SaveSettings ( settings );
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
				if ( value < 0 || value > 100 ) // Ensure volume is within acceptable range
					throw new ArgumentOutOfRangeException ( nameof ( Volume ), "Volume must be between 0 and 100." );

				var settings = _settingsService.GetCurrentSettings();
				if ( settings.Volume != value )
				{
					settings.Volume = value;
					_settingsService.SaveSettings ( settings );
					_ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, value ); // Live update
					OnPropertyChanged ( nameof ( Volume ) );
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
					_settingsService.SaveSettings ( settings );
					_ttsService.SetTTSParameters ( value, settings.VoiceRate, settings.Volume ); // Live update
					OnPropertyChanged ( nameof ( Voice ) );
				}
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
					UpdateHotkey ( value );
				}
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

			var modifiers = parts.Take(parts.Length - 1).ToArray();
			var key = parts.Last();

			if ( !Enum.TryParse ( key, true, out Key newKey ) )
			{
				throw new ArgumentException ( $"Invalid key value: {key}" );
			}

			var settings = _settingsService.GetCurrentSettings();
			var newHotkey = string.Join("+", modifiers) + "+" + newKey;

			if ( settings.Hotkey.Key != newHotkey )
			{
				try
				{
					_hotkeyService.UnregisterHotkey ( ); // Unregister previous hotkey
					var virtualKey = KeyInterop.VirtualKeyFromKey(newKey);
					_hotkeyService.RegisterHotkey ( string.Join ( "+", modifiers ), newKey.ToString ( ) );

					settings.Hotkey.Modifiers = string.Join ( "+", modifiers );
					settings.Hotkey.Key = newKey.ToString ( );
					_settingsService.SaveSettings ( settings );
					OnPropertyChanged ( nameof ( HotkeyCombination ) );
				}
				catch ( Exception ex )
				{
					MessageBox.Show ( $"Failed to register hotkey: {hotkeyCombination}. It might already be in use by another application.", "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
					Logger.Warn ( $"Failed to register hotkey: {hotkeyCombination}. It might already be in use by another application." );
				}

				var ttsSettings = _settingsService.GetCurrentSettings();
				_ttsService.SetTTSParameters ( ttsSettings.Voice, ttsSettings.VoiceRate, ttsSettings.Volume );
			}
		}

		protected void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
		}
	}
}
