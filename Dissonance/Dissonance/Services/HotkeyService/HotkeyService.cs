using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Dissonance.Infrastructure.Constants;
using Dissonance.ViewModels;

using Microsoft.Extensions.Logging;

using NLog;

namespace Dissonance.Services.HotkeyService
{
	internal class HotkeyService : IHotkeyService, IDisposable
	{
		private readonly ILogger<HotkeyService> _logger;
		private readonly Dissonance.Services.MessageService.IMessageService _messageService;

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

		public HotkeyService ( ILogger<HotkeyService> logger, Dissonance.Services.MessageService.IMessageService messageService )
		{
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
			_messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
		}

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
					case "alt": mod |= Infrastructure.Constants.ModifierKeys.Alt; break;
					case "ctrl": mod |= Infrastructure.Constants.ModifierKeys.Control; break;
					case "shift": mod |= Infrastructure.Constants.ModifierKeys.Shift; break;
					case "win": mod |= Infrastructure.Constants.ModifierKeys.Win; break;
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
			if ( msg == WindowsMessages.Hotkey )
			{
				_logger.LogInformation( "Hotkey pressed." );
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
			_logger.LogInformation( "HotkeyService disposed." );
		}

		public void Initialize ( Window mainWindow )
		{
			if ( mainWindow == null )
			{
				throw new ArgumentNullException ( nameof ( mainWindow ), "MainWindow cannot be null." );
			}

			var helper = new WindowInteropHelper(mainWindow);
			_windowHandle = helper.Handle;

			if ( _windowHandle == IntPtr.Zero )
			{
				throw new InvalidOperationException ( "Failed to get a valid window handle." );
			}

			_source = HwndSource.FromHwnd ( _windowHandle );
			_source.AddHook ( WndProc );
		}

		public void RegisterHotkey ( AppSettings.HotkeySettings hotkey )
		{
			if ( hotkey == null ) throw new ArgumentNullException ( nameof ( hotkey ) );
			lock ( _lock )
			{
				try
				{
					uint mod = ParseModifiers(hotkey.Modifiers);
					uint vk = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), hotkey.Key, true));

					if ( _currentHotkeyId.HasValue )
					{
						UnregisterHotkey ( );
					}

					int hotkeyId = _nextHotkeyId++;

					if ( RegisterHotKey ( _windowHandle, hotkeyId, mod, vk ) )
					{
						_currentHotkeyId = hotkeyId;
						_logger.LogInformation( $"Hotkey registered: {hotkey.Modifiers} + {hotkey.Key}" );
					}
					else
					{
						_messageService.DissonanceMessageBoxShowWarning ( "Hotkey Registration Failure", $"Failed to register hotkey: {hotkey.Modifiers} + {hotkey.Key}. It might already be in use by another application.");
					}
				}
				catch ( ArgumentException ex )
				{
					_messageService.DissonanceMessageBoxShowError( "Hotkey Registration Failure", $"Failed to register hotkey: {hotkey.Modifiers} + {hotkey.Key}.", ex);
				}
				catch ( Exception ex )
				{
					_messageService.DissonanceMessageBoxShowError ( "Hotkey Registration Failure", $"Failed to register hotkey: {hotkey.Modifiers} + {hotkey.Key}. An unexpected error occurred.", ex );
				}
			}
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
						_logger.LogInformation( $"Hotkey unregistered with Id: {hotkeyId}" );
					}
					else
					{
						_logger.LogWarning( $"Failed to unregister hotkey with id: {hotkeyId}" );
					}
					_currentHotkeyId = null;
				}
			}
		}
	}
}