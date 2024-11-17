namespace Dissonance.Services.SettingsService
{
	public interface ISettingsService
	{
		AppSettings GetCurrentSettings ( );

		AppSettings LoadSettings ( );

		void ResetToFactorySettings ( );

		void SaveSettings ( AppSettings settings );
	}
}