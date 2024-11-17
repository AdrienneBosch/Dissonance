namespace Dissonance
{
	public class AppSettings
	{
		public HotkeySettings Hotkey { get; set; }

		public string Voice { get; set; }

		public double VoiceRate { get; set; }

		public int Volume { get; set; }

		public class HotkeySettings
		{
			public string Key { get; set; }

			public string Modifiers { get; set; }
		}
	}
}