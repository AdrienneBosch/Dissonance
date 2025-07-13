using System;
using System.Windows;

namespace Dissonance.Services.HotkeyService
{
	public interface IHotkeyService : IDisposable
	{
		void Initialize(Window mainWindow);
		void RegisterHotkey(string id, AppSettings.HotkeySettings hotkey, Action callback);
		void UnregisterHotkey(string id);
	}
}