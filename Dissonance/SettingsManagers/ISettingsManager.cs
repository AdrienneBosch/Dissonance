using System.Threading.Tasks;

namespace Dissonance.SettingsManagers
{
	public interface ISettingsManager
	{
		Task<AppSettings> LoadSettingsAsync ( string customFilePath = null );
		Task SaveSettingsAsync ( AppSettings settings, string customFilePath = null );
		Task SaveAsDefaultConfigurationAsync ( AppSettings settings );
		Task<AppSettings> LoadFactoryDefaultAsync ( );
	}
}
