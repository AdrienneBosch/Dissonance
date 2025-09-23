namespace Dissonance.Services.SettingsService
{
public interface ISettingsService
{
AppSettings GetCurrentSettings ( );

AppSettings LoadSettings ( );

void ResetToFactorySettings ( );

void SaveSettings ( AppSettings settings );

bool SaveCurrentSettings ( );

bool SaveCurrentSettingsAsDefault ( );

bool ExportSettings ( string destinationPath );

bool ImportSettings ( string sourcePath );
}
}