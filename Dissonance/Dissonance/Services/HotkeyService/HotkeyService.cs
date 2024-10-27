using System;
using System.Runtime.InteropServices;  // For calling Windows API
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;  // For managing windows and messages

using NLog;

namespace Dissonance.Services.HotkeyService
{
	internal class HotkeyService : IHotkeyService, IDisposable
	{
		// Constants for Windows API functions
		private const int WM_HOTKEY = 0x0312;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private IntPtr _windowHandle;
		private HwndSource _source;

		public event Action HotkeyPressed;

		// Use the Windows API to register and unregister hotkeys
		[DllImport ( "user32.dll", SetLastError = true )]
		private static extern bool RegisterHotKey ( IntPtr hWnd, int id, uint fsModifiers, uint vk );

		[DllImport ( "user32.dll", SetLastError = true )]
		private static extern bool UnregisterHotKey ( IntPtr hWnd, int id );

		// Modifier key flags
		private const uint MOD_ALT = 0x0001;
		private const uint MOD_CONTROL = 0x0002;
		private const uint MOD_SHIFT = 0x0004;
		private const uint MOD_WIN = 0x0008;

		// Registered hotkey ID
		private int _hotkeyId = 0;

		// Method to initialize the hotkey service with the main window
		public void Initialize ( Window mainWindow )
		{
			var helper = new WindowInteropHelper(mainWindow);
			_windowHandle = helper.Handle;

			// Attach the Windows message handler
			_source = HwndSource.FromHwnd ( _windowHandle );
			_source.AddHook ( WndProc );
		}

		// Register hotkey with the specified modifiers and key
		public void RegisterHotkey ( string modifiers, string key )
		{
			try
			{
				uint mod = ParseModifiers(modifiers);
				uint vk = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key));

				_hotkeyId++;  // Increment hotkey ID for unique registration

				if ( RegisterHotKey ( _windowHandle, _hotkeyId, mod, vk ) )
				{
					Logger.Info ( $"Hotkey registered: {modifiers} + {key}" );
				}
				else
				{
					Logger.Error ( "Failed to register hotkey." );
				}
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to register hotkey." );
			}
		}

		// Unregister hotkey
		public void UnregisterHotkey ( )
		{
			try
			{
				if ( UnregisterHotKey ( _windowHandle, _hotkeyId ) )
				{
					Logger.Info ( "Hotkey unregistered." );
				}
				else
				{
					Logger.Error ( "Failed to unregister hotkey." );
				}
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to unregister hotkey." );
			}
		}

		// Parse the modifier string and return the corresponding flags
		private uint ParseModifiers ( string modifiers )
		{
			uint mod = 0;
			if ( modifiers.Contains ( "Alt" ) ) mod |= MOD_ALT;
			if ( modifiers.Contains ( "Ctrl" ) ) mod |= MOD_CONTROL;
			if ( modifiers.Contains ( "Shift" ) ) mod |= MOD_SHIFT;
			if ( modifiers.Contains ( "Win" ) ) mod |= MOD_WIN;
			return mod;
		}

		// Process Windows messages to detect hotkey press
		private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			if ( msg == WM_HOTKEY )
			{
				Logger.Info ( "Hotkey pressed." );
				HotkeyPressed?.Invoke ( );  // Trigger event when the hotkey is pressed
				handled = true;
			}
			return IntPtr.Zero;
		}

		// Dispose resources
		public void Dispose ( )
		{
			_source?.RemoveHook ( WndProc );
			UnregisterHotkey ( );
		}
	}
}
