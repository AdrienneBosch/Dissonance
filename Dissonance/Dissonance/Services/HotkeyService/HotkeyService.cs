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
		private readonly object _lock = new object ( );
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
			if ( string.IsNullOrWhiteSpace ( modifiers ) )
				throw new ArgumentException ( "Modifiers cannot be null or empty.", nameof ( modifiers ) );

			uint mod = 0;
			var parts = modifiers.Split(new[] { '+', ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach ( var part in parts.Select ( p => p.Trim ( ) ) )
			{
				switch ( part.ToLower ( ) )
				{
					case "alt": mod |= MOD_ALT; break;
					case "ctrl": mod |= MOD_CONTROL; break;
					case "shift": mod |= MOD_SHIFT; break;
					case "win": mod |= MOD_WIN; break;
					default:
						throw new ArgumentException ( $"Unknown modifier: {part}", nameof ( modifiers ) );
				}
			}

			if ( mod == 0 )
				throw new ArgumentException ( "Hotkey must include at least one modifier." );

			return mod;
		}

		private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			if ( msg == WM_HOTKEY )
			{
				Logger.Info ( "Hotkey pressed." );
				var handler = HotkeyPressed;
				if ( handler != null )
				{
					Application.Current.Dispatcher.BeginInvoke ( handler );
				}
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
			lock ( _lock )
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
		}

		public void RegisterHotkey ( AppSettings.HotkeySettings hotkey )
		{
			if ( hotkey == null ) throw new ArgumentNullException ( nameof ( hotkey ) );
			RegisterHotkey ( hotkey.Modifiers, hotkey.Key );
		}

		public void UnregisterHotkey ( )
		{
			lock ( _lock )
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
}