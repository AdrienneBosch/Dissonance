using System.Collections.ObjectModel;
using System.ComponentModel;

using Dissonance.Services.SettingsService;

namespace Dissonance.ViewModels
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private readonly ISettingsService _settingsService;

		public event PropertyChangedEventHandler PropertyChanged;
		public ObservableCollection<string> AvailableVoices { get; } = new ObservableCollection<string> ( );



		public MainWindowViewModel ( ISettingsService settingsService )
		{
			_settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
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

		protected void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
		}
	}
}