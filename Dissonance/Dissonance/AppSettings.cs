namespace Dissonance
{
        public class AppSettings
        {
                public HotkeySettings Hotkey { get; set; }

                public string Voice { get; set; }

                public double VoiceRate { get; set; }

                public int Volume { get; set; }

                public bool SaveConfigAsDefaultOnClose { get; set; }

                public bool UseDarkTheme { get; set; }

                public double? WindowLeft { get; set; }

                public double? WindowTop { get; set; }

                public double? WindowWidth { get; set; }

                public double? WindowHeight { get; set; }

                public bool IsWindowMaximized { get; set; }

                public class HotkeySettings
                {
                        public string Key { get; set; }

                        public string Modifiers { get; set; }
                }
        }
}
