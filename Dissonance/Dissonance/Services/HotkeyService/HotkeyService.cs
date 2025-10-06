using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using Dissonance.Infrastructure.Constants;

using Microsoft.Extensions.Logging;

namespace Dissonance.Services.HotkeyService
{
        internal class HotkeyService : IHotkeyService, IDisposable
        {
                private readonly object _lock = new object ( );
                private readonly ILogger<HotkeyService> _logger;
                private readonly Dissonance.Services.MessageService.IMessageService _messageService;
                private readonly Action<Action> _dispatcherInvoker;
                private readonly Dictionary<int, Action> _hotkeyCallbacks = new ( );
                private int? _primaryHotkeyId;
                private int _nextHotkeyId = 0;
                private HwndSource? _source;
                private IntPtr _windowHandle;

                public HotkeyService ( ILogger<HotkeyService> logger, Dissonance.Services.MessageService.IMessageService messageService )
                        : this ( logger, messageService, null )
                {
                }

                internal HotkeyService ( ILogger<HotkeyService> logger, Dissonance.Services.MessageService.IMessageService messageService, Action<Action> dispatcherInvoker )
                {
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
                        _dispatcherInvoker = dispatcherInvoker ?? InvokeOnApplicationDispatcher;
                }

		public event Action HotkeyPressed;

		[DllImport ( "user32.dll" )]
		private static extern bool RegisterHotKey ( IntPtr hWnd, int id, uint fsModifiers, uint vk );

		[DllImport ( "user32.dll" )]
		private static extern bool UnregisterHotKey ( IntPtr hWnd, int id );

                private uint ParseModifiers ( string modifiers, bool allowEmptyModifiers )
                {
                        if ( string.IsNullOrWhiteSpace ( modifiers ) )
                        {
                                if ( allowEmptyModifiers )
                                {
                                        return 0;
                                }

                                throw new ArgumentException ( "Modifiers cannot be null or empty.", nameof ( modifiers ) );
                        }

                        uint mod = 0;
                        var parts = modifiers.Split ( new[] { '+', ',' }, StringSplitOptions.RemoveEmptyEntries );

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
                        {
                                if ( allowEmptyModifiers )
                                {
                                        return 0;
                                }

                                throw new ArgumentException ( "Hotkey must include at least one modifier." );
                        }

                        return mod;
                }

                private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
                {
                        if ( msg == WindowsMessages.Hotkey )
                        {
                                var hotkeyId = wParam.ToInt32 ( );
                                var wasHandled = false;

                                if ( _primaryHotkeyId.HasValue && hotkeyId == _primaryHotkeyId.Value )
                                {
                                        _logger.LogInformation ( "Primary hotkey pressed." );
                                        var handler = HotkeyPressed;
                                        if ( handler != null )
                                        {
                                                _dispatcherInvoker ( handler );
                                        }
                                        wasHandled = true;
                                }

                                if ( _hotkeyCallbacks.TryGetValue ( hotkeyId, out var callback ) )
                                {
                                        _logger.LogInformation ( "Hotkey pressed for registration {HotkeyId}.", hotkeyId );
                                        _dispatcherInvoker ( callback );
                                        wasHandled = true;
                                }

                                handled = wasHandled;
                        }

                        return IntPtr.Zero;
                }

                public void Dispose ( )
                {
                        lock ( _lock )
                        {
                                foreach ( var registrationId in _hotkeyCallbacks.Keys.ToList ( ) )
                                {
                                        UnregisterHotkeyInternal ( registrationId );
                                }

                                _hotkeyCallbacks.Clear ( );

                                if ( _primaryHotkeyId.HasValue )
                                {
                                        UnregisterHotkeyInternal ( _primaryHotkeyId.Value );
                                        _primaryHotkeyId = null;
                                }
                        }

                        _source?.RemoveHook ( WndProc );
                        _source = null;
                        _logger.LogInformation ( "HotkeyService disposed." );
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
                                if ( _primaryHotkeyId.HasValue )
                                {
                                        UnregisterHotkeyInternal ( _primaryHotkeyId.Value );
                                        _primaryHotkeyId = null;
                                }

                                var registrationId = RegisterHotkeyInternal ( hotkey, allowEmptyModifiers: false );
                                if ( registrationId.HasValue )
                                {
                                        _primaryHotkeyId = registrationId.Value;
                                }
                        }
                }

                public IDisposable? RegisterHotkey ( AppSettings.HotkeySettings hotkey, Action callback, bool allowEmptyModifiers = false )
                {
                        if ( hotkey == null ) throw new ArgumentNullException ( nameof ( hotkey ) );
                        if ( callback == null ) throw new ArgumentNullException ( nameof ( callback ) );

                        lock ( _lock )
                        {
                                var registrationId = RegisterHotkeyInternal ( hotkey, allowEmptyModifiers );
                                if ( !registrationId.HasValue )
                                {
                                        return null;
                                }

                                var hotkeyId = registrationId.Value;
                                _hotkeyCallbacks[hotkeyId] = callback;
                                return new HotkeyRegistration ( this, hotkeyId );
                        }
                }

                public void UnregisterHotkey ( )
                {
                        lock ( _lock )
                        {
                                if ( _primaryHotkeyId.HasValue )
                                {
                                        UnregisterHotkeyInternal ( _primaryHotkeyId.Value );
                                        _primaryHotkeyId = null;
                                }
                        }
                }

                private int? RegisterHotkeyInternal ( AppSettings.HotkeySettings hotkey, bool allowEmptyModifiers )
                {
                        try
                        {
                                if ( _windowHandle == IntPtr.Zero )
                                {
                                        throw new InvalidOperationException ( "HotkeyService has not been initialized with a window handle." );
                                }

                                if ( string.IsNullOrWhiteSpace ( hotkey.Key ) )
                                {
                                        throw new ArgumentException ( "Hotkey must include a key.", nameof ( hotkey ) );
                                }

                                var modifiers = ParseModifiers ( hotkey.Modifiers ?? string.Empty, allowEmptyModifiers );
                                var key = ( Key ) Enum.Parse ( typeof ( Key ), hotkey.Key, true );
                                var virtualKey = ( uint ) KeyInterop.VirtualKeyFromKey ( key );
                                var hotkeyId = _nextHotkeyId++;

                                if ( RegisterHotKey ( _windowHandle, hotkeyId, modifiers, virtualKey ) )
                                {
                                        _logger.LogDebug ( "Hotkey registered: {Hotkey}", DescribeHotkey ( hotkey ) );
                                        return hotkeyId;
                                }

                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.HotkeyServiceWarning,
                                        $"Failed to register hotkey: {DescribeHotkey ( hotkey )}. It might already be in use by another application." );
                        }
                        catch ( ArgumentException ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                        $"Failed to register hotkey: {DescribeHotkey ( hotkey )}.", ex );
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                        $"Failed to register hotkey: {DescribeHotkey ( hotkey )}. An unexpected error occurred.", ex );
                        }

                        return null;
                }

                private void UnregisterAdditionalHotkey ( int hotkeyId )
                {
                        lock ( _lock )
                        {
                                if ( _hotkeyCallbacks.Remove ( hotkeyId ) )
                                {
                                        UnregisterHotkeyInternal ( hotkeyId );
                                }
                        }
                }

                private void UnregisterHotkeyInternal ( int hotkeyId )
                {
                        if ( _windowHandle == IntPtr.Zero )
                        {
                                return;
                        }

                        if ( UnregisterHotKey ( _windowHandle, hotkeyId ) )
                        {
                                _logger.LogDebug ( "Hotkey unregistered with Id: {HotkeyId}", hotkeyId );
                        }
                        else
                        {
                                _logger.LogWarning ( "Failed to unregister hotkey with id: {HotkeyId}", hotkeyId );
                        }
                }

                private static string DescribeHotkey ( AppSettings.HotkeySettings hotkey )
                {
                        if ( hotkey == null )
                        {
                                return string.Empty;
                        }

                        var keyPart = string.IsNullOrWhiteSpace ( hotkey.Key ) ? "(none)" : hotkey.Key.Trim ( );
                        return string.IsNullOrWhiteSpace ( hotkey.Modifiers )
                                ? keyPart
                                : $"{hotkey.Modifiers.Trim ( )} + {keyPart}";
                }

                private static void InvokeOnApplicationDispatcher ( Action action )
                {
                        if ( action == null )
                                throw new ArgumentNullException ( nameof ( action ) );

                        var dispatcher = Application.Current?.Dispatcher;
                        if ( dispatcher != null && !dispatcher.HasShutdownStarted )
                        {
                                dispatcher.BeginInvoke ( action, DispatcherPriority.Normal );
                        }
                        else
                        {
                                action ( );
                        }
                }

                private sealed class HotkeyRegistration : IDisposable
                {
                        private readonly HotkeyService _service;
                        private readonly int _hotkeyId;
                        private bool _disposed;

                        public HotkeyRegistration ( HotkeyService service, int hotkeyId )
                        {
                                _service = service;
                                _hotkeyId = hotkeyId;
                        }

                        public void Dispose ( )
                        {
                                if ( _disposed )
                                {
                                        return;
                                }

                                _service.UnregisterAdditionalHotkey ( _hotkeyId );
                                _disposed = true;
                        }
                }
        }
}
