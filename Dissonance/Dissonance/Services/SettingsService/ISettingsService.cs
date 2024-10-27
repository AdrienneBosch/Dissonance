using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.Services.SettingsService
{
    internal interface ISettingsService
	{
		AppSettings LoadSettings ( );
		void SaveSettings ( AppSettings settings );
		void ResetToFactorySettings ( );
		AppSettings GetCurrentSettings ( );
	}
}
