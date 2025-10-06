using System;
using System.Windows;
using System.Windows.Input;

namespace Dissonance.Services.DocumentReaderHotkeyService
{
        public interface IDocumentReaderHotkeyService : IDisposable
        {
                event Action? HotkeyPressed;

                void Initialize ( Window mainWindow );

                bool RegisterHotkey ( ModifierKeys modifiers, Key key );

                void UnregisterHotkey ( );
        }
}
