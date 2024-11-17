using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.Services.HotkeyService
{
	public interface IHotkeyService
	{
		/// <summary>
		/// Event that gets triggered when the hotkey is pressed.
		/// </summary>
		event Action HotkeyPressed;

		/// <summary>
		/// Registers the hotkey using modifiers and a key.
		/// </summary>
		/// <param name="modifiers">Modifier keys like Ctrl, Alt, Shift.</param>
		/// <param name="key">Main key to trigger the hotkey.</param>
		void RegisterHotkey ( string modifiers, string key );

		/// <summary>
		/// Unregisters the currently registered hotkey.
		/// </summary>
		void UnregisterHotkey ( );
	}
}

