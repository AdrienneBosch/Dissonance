using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using NLog;

namespace Dissonance.Services.HotkeyService
{
	internal class HotkeyService : IHotkeyService, IDisposable
	{
		private const int WM_HOTKEY = 0x0312;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private IntPtr _windowHandle;
		private HwndSource _source;

		public event Action HotkeyPressed;

		[DllImport ( "user32.dll", SetLastError = true )]
		private static extern bool RegisterHotKey ( IntPtr hWnd, int id, uint fsModifiers, uint vk );

		[DllImport ( "user32.dll", SetLastError = true )]
		private static extern bool UnregisterHotKey ( IntPtr hWnd, int id );

		private const uint MOD_ALT = 0x0001;
		private const uint MOD_CONTROL = 0x0002;
		private const uint MOD_SHIFT = 0x0004;
		private const uint MOD_WIN = 0x0008;

		private int _nextHotkeyId = 1;
		private int? _currentHotkeyId;

		public void Initialize ( Window mainWindow )
		{
			var helper = new WindowInteropHelper(mainWindow);
			_windowHandle = helper.Handle;
			_source = HwndSource.FromHwnd ( _windowHandle );
			_source.AddHook ( WndProc );
		}

		public void RegisterHotkey ( string modifiers, string key )
		{
			try
			{
				uint mod = ParseModifiers(modifiers);
				uint vk = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key));

				if ( _currentHotkeyId.HasValue )
				{
					UnregisterHotkey ( ); // Unregister any previous hotkey
				}

				int hotkeyId = _nextHotkeyId++;

				if ( RegisterHotKey ( _windowHandle, hotkeyId, mod, vk ) )
				{
					_currentHotkeyId = hotkeyId;
					Logger.Info ( $"Hotkey registered: {modifiers} + {key}" );
				}
				else
				{
					Logger.Error ( "Failed to register hotkey. It might already be in use by another application." );
				}
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to register hotkey." );
			}
		}

		public void UnregisterHotkey ( )
		{
			try
			{
				if ( _currentHotkeyId.HasValue && UnregisterHotKey ( _windowHandle, _currentHotkeyId.Value ) )
				{
					Logger.Info ( "Hotkey unregistered." );
					_currentHotkeyId = null;
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

		private uint ParseModifiers ( string modifiers )
		{
			uint mod = 0;
			if ( modifiers.Contains ( "Alt" ) ) mod |= MOD_ALT;
			if ( modifiers.Contains ( "Ctrl" ) ) mod |= MOD_CONTROL;
			if ( modifiers.Contains ( "Shift" ) ) mod |= MOD_SHIFT;
			if ( modifiers.Contains ( "Win" ) ) mod |= MOD_WIN;
			return mod;
		}

		private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			if ( msg == WM_HOTKEY )
			{
				Logger.Info ( "Hotkey pressed." );
				HotkeyPressed?.Invoke ( );
				handled = true;
			}
			return IntPtr.Zero;
		}

		public void Dispose ( )
		{
			_source?.RemoveHook ( WndProc );
			UnregisterHotkey ( );
		}
	}
}