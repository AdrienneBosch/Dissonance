using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.SettingsManagers
{
	public interface ISettingsManager
	{
		AppSettings LoadSettings ( );
		void SaveSettings ( AppSettings settings );
	}

}
