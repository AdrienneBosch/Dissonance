using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using NLog;

namespace Dissonance.Services.HotkeyService
{
	internal class HotkeyService : IHotkeyService, IDisposable
	{
		private const uint MOD_ALT = 0x0001;
		private const uint MOD_CONTROL = 0x0002;
		private const uint MOD_SHIFT = 0x0004;
		private const uint MOD_WIN = 0x0008;
		private const int WM_HOTKEY = 0x0312;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );
		private int? _currentHotkeyId;
		private int _nextHotkeyId = 0;
		private HwndSource _source;
		private IntPtr _windowHandle;
		public event Action HotkeyPressed;

		[DllImport ( "user32.dll" )]
		private static extern bool RegisterHotKey ( IntPtr hWnd, int id, uint fsModifiers, uint vk );

		[DllImport ( "user32.dll" )]
		private static extern bool UnregisterHotKey ( IntPtr hWnd, int id );

		private uint ParseModifiers ( string modifiers )
		{
			uint mod = 0;
			if ( modifiers.Contains ( "Alt" ) ) mod |= MOD_ALT;
			if ( modifiers.Contains ( "Ctrl" ) ) mod |= MOD_CONTROL;
			if ( modifiers.Contains ( "Shift" ) ) mod |= MOD_SHIFT;
			if ( modifiers.Contains ( "Win" ) ) mod |= MOD_WIN;

			if ( mod == 0 )
			{
				throw new ArgumentException ( "Hotkey must include at least one modifier." );
			}

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
			Logger.Info ( "HotkeyService disposed." );
		}

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
				uint vk = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key, true));

				if ( _currentHotkeyId.HasValue )
				{
					UnregisterHotkey ( );
				}

				int hotkeyId = _nextHotkeyId++;

				if ( RegisterHotKey ( _windowHandle, hotkeyId, mod, vk ) )
				{
					_currentHotkeyId = hotkeyId;
					Logger.Info ( $"Hotkey registered: {modifiers} + {key}" );
				}
				else
				{
					string errorMessage = $"Failed to register hotkey: {modifiers} + {key}. It might already be in use by another application.";
					Logger.Warn ( errorMessage );
					MessageBox.Show ( errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
				}
			}
			catch ( ArgumentException ex )
			{
				string errorMessage = $"Failed to register hotkey: {modifiers} + {key}. {ex.Message}";
				Logger.Warn ( errorMessage );
				MessageBox.Show ( errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
			catch ( Exception ex )
			{
				string errorMessage = $"Failed to register hotkey: {modifiers} + {key}. An unexpected error occurred.";
				Logger.Error ( ex, errorMessage );
				MessageBox.Show ( errorMessage, "Hotkey Registration Error", MessageBoxButton.OK, MessageBoxImage.Error );
				throw;
			}
		}

		public void UnregisterHotkey ( )
		{
			if ( _currentHotkeyId.HasValue )
			{
				var hotkeyId = _currentHotkeyId.Value;
				if ( UnregisterHotKey ( _windowHandle, hotkeyId ) )
				{
					Logger.Info ( $"Hotkey unregistered with Id: {hotkeyId}" );
				}
				else
				{
					Logger.Warn ( $"Failed to unregister hotkey with id: {hotkeyId}" );
				}
				_currentHotkeyId = null;
			}
		}
	}
}