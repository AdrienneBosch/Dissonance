namespace Dissonance.Services.HotkeyService
{
	public interface IHotkeyService
	{
		event Action HotkeyPressed;

		void RegisterHotkey ( AppSettings.HotkeySettings hotkey );

		void UnregisterHotkey ( );
	}
}