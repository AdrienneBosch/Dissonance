using System;

namespace Dissonance
{
        public class AppSettings
        {
                public HotkeySettings Hotkey { get; set; }

                public DocumentReaderHotkeySettings DocumentReaderHotkey { get; set; }

                public string? DocumentReaderHighlightColor { get; set; }

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

                public bool RememberDocumentReaderPosition { get; set; }

                public string? DocumentReaderLastFilePath { get; set; }

                public int DocumentReaderLastCharacterIndex { get; set; }

                public DocumentReaderResumeState? DocumentReaderResumeState { get; set; }

                public class HotkeySettings
                {
                        public string Key { get; set; }

                        public string Modifiers { get; set; }

                        public bool AutoReadClipboard { get; set; }
                }

                public class DocumentReaderHotkeySettings
                {
                        public string Key { get; set; }

                        public string Modifiers { get; set; }

                        public bool UsePlayPauseToggle { get; set; }
                }

                public class DocumentReaderResumeState
                {
                        public string? FilePath { get; set; }

                        public int CharacterIndex { get; set; }

                        public int DocumentLength { get; set; }

                        public string? ContentHash { get; set; }

                        public long? FileSize { get; set; }

                        public DateTime? LastWriteTimeUtc { get; set; }
                }
        }
}
