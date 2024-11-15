using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

using Dissonance.Services.HotkeyService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;

namespace Dissonance.ViewModels
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private readonly ISettingsService _settingsService;

		public event PropertyChangedEventHandler PropertyChanged;
		public ObservableCollection<string> AvailableVoices { get; } = new ObservableCollection<string> ( );

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
		}


		public double VoiceRate
		{
			get => _settingsService.GetCurrentSettings ( ).VoiceRate;
			set
			{
				var settings = _settingsService.GetCurrentSettings();
				settings.VoiceRate = value;
				_settingsService.SaveSettings ( settings );
				OnPropertyChanged ( nameof ( VoiceRate ) );
			}
		}

		public int Volume
		{
			get => _settingsService.GetCurrentSettings ( ).Volume;
			set
			{
				var settings = _settingsService.GetCurrentSettings();
				settings.Volume = value;
				_settingsService.SaveSettings ( settings );
				OnPropertyChanged ( nameof ( Volume ) );

				_ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
			}
		}

		public string Voice
		{
			get => _settingsService.GetCurrentSettings ( ).Voice;
			set
			{
				var settings = _settingsService.GetCurrentSettings();
				settings.Voice = value;
				_settingsService.SaveSettings ( settings );
				OnPropertyChanged ( nameof ( Voice ) );
			}
		}

		public string Hotkey
		{
			get => _settingsService.GetCurrentSettings ( ).Hotkey.Key; // Return the string representation
			set
			{
				if ( Enum.TryParse ( value, true, out Key newKey ) ) // Parse input to Key enum
				{
					var settings = _settingsService.GetCurrentSettings();
					settings.Hotkey.Key = newKey.ToString ( ); // Convert Key enum to string for assignment
					_settingsService.SaveSettings ( settings );
					OnPropertyChanged ( nameof ( Hotkey ) );

					// Convert Key to virtual key code for HotkeyService
					var virtualKey = KeyInterop.VirtualKeyFromKey(newKey);
					_hotkeyService?.RegisterHotkey ( settings.Hotkey.Modifiers, virtualKey.ToString ( ) );
				}
				else
				{
					throw new ArgumentException ( $"Invalid key value: {value}" );
				}
			}
		}



		protected void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
		}
	}
}