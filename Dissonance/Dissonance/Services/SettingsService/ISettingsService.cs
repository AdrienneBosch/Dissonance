namespace Dissonance.Services.SettingsService
{
	public interface ISettingsService
	{
		AppSettings LoadSettings ( );
		void SaveSettings ( AppSettings settings );
		void ResetToFactorySettings ( );
		AppSettings GetCurrentSettings ( );
	}
}
