using System.Windows;

namespace Dissonance.Services.HotkeyService
{
        public interface IHotkeyService : IDisposable
        {
                event Action HotkeyPressed;

                void Initialize ( Window mainWindow );

                void RegisterHotkey ( AppSettings.HotkeySettings hotkey );

                IDisposable? RegisterHotkey ( AppSettings.HotkeySettings hotkey, Action callback, bool allowEmptyModifiers = false );

                void UnregisterHotkey ( );
        }
}