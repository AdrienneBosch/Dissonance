using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissonance.Services.HotkeyService
{
	public interface IHotkeyService
	{
		event Action HotkeyPressed;

		void RegisterHotkey ( string modifiers, string key );
		void RegisterHotkey ( AppSettings.HotkeySettings hotkey ); 
		void UnregisterHotkey ( );
	}
}