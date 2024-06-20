using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

using Formatting = Newtonsoft.Json.Formatting;

namespace Dissonance.SetttingsManager
{
	public class SettingsManager : ISettingsManager
	{
		private const string SettingsFilePath = "settings.json";
		private const string DefaultSettingsFilePath = "defaultSettings.json";

		public AppSettings LoadSettings ( )
		{
			EnsureSettingsFileExists ( );

			var json = File.ReadAllText(SettingsFilePath);
			return JsonConvert.DeserializeObject<AppSettings> ( json );
		}

		public void SaveSettings ( AppSettings settings )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			File.WriteAllText ( SettingsFilePath, json );
		}

		private void EnsureSettingsFileExists ( )
		{
			if ( !File.Exists ( SettingsFilePath ) )
			{
				File.Copy ( DefaultSettingsFilePath, SettingsFilePath );
			}
		}
	}
}
