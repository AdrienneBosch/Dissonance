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

		public AppSettings LoadSettings ( )
		{
			if ( !File.Exists ( SettingsFilePath ) )
			{
				return new AppSettings ( ); // Return default settings if file doesn't exist
			}

			var json = File.ReadAllText(SettingsFilePath);
			return JsonConvert.DeserializeObject<AppSettings> ( json );
		}

		public void SaveSettings ( AppSettings settings )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			File.WriteAllText ( SettingsFilePath, json );
		}
	}
}
