using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.SettingsManagers
{
	public interface ISettingsManager
	{
		AppSettings LoadSettings ( string customFilePath = null );
		void SaveSettings ( AppSettings settings, string customFilePath = null );
		void SaveAsDefaultConfiguration ( AppSettings settings );
		AppSettings LoadFactoryDefault ( );
	}
}
