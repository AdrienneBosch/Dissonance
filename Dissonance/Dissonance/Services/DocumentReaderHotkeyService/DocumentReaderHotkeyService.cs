using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using Dissonance.Services.MessageService;

using Microsoft.Extensions.Logging;

using WindowsMessages = Dissonance.Infrastructure.Constants.WindowsMessages;
using HotkeyModifierKeys = Dissonance.Infrastructure.Constants.ModifierKeys;
using MessageBoxTitles = Dissonance.Infrastructure.Constants.MessageBoxTitles;

namespace Dissonance.Services.DocumentReaderHotkeyService
{
        internal class DocumentReaderHotkeyService : IDocumentReaderHotkeyService
        {
                private readonly object _lock = new();
                private readonly ILogger<DocumentReaderHotkeyService> _logger;
                private readonly IMessageService _messageService;
                private readonly Action<Action> _dispatcherInvoker;
                private HwndSource? _source;
                private IntPtr _windowHandle;
                private int? _currentHotkeyId;
                private int _nextHotkeyId;

                public DocumentReaderHotkeyService ( ILogger<DocumentReaderHotkeyService> logger, IMessageService messageService )
                        : this ( logger, messageService, null )
                {
                }

                internal DocumentReaderHotkeyService ( ILogger<DocumentReaderHotkeyService> logger, IMessageService messageService, Action<Action>? dispatcherInvoker )
                {
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
                        _dispatcherInvoker = dispatcherInvoker ?? InvokeOnApplicationDispatcher;
                }

                public event Action? HotkeyPressed;

                [DllImport ( "user32.dll" )]
                private static extern bool RegisterHotKey ( IntPtr hWnd, int id, uint fsModifiers, uint vk );

                [DllImport ( "user32.dll" )]
                private static extern bool UnregisterHotKey ( IntPtr hWnd, int id );

                public void Initialize ( Window mainWindow )
                {
                        if ( mainWindow == null )
                                throw new ArgumentNullException ( nameof ( mainWindow ), "MainWindow cannot be null." );

                        var helper = new WindowInteropHelper ( mainWindow );
                        _windowHandle = helper.Handle;

                        if ( _windowHandle == IntPtr.Zero )
                                throw new InvalidOperationException ( "Failed to get a valid window handle." );

                        _source = HwndSource.FromHwnd ( _windowHandle );
                        _source.AddHook ( WndProc );
                }

                public bool RegisterHotkey ( ModifierKeys modifiers, Key key )
                {
                        if ( key == Key.None )
                                throw new ArgumentException ( "Key cannot be Key.None when registering a hotkey.", nameof ( key ) );

                        if ( _windowHandle == IntPtr.Zero )
                                throw new InvalidOperationException ( "The hotkey service must be initialized before registering a hotkey." );

                        lock ( _lock )
                        {
                                try
                                {
                                        uint modifierFlags = ConvertModifiers ( modifiers );
                                        uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey ( key );

                                        if ( _currentHotkeyId.HasValue )
                                        {
                                                UnregisterHotkeyInternal ( );
                                        }

                                        var hotkeyId = _nextHotkeyId++;
                                        if ( RegisterHotKey ( _windowHandle, hotkeyId, modifierFlags, virtualKey ) )
                                        {
                                                _currentHotkeyId = hotkeyId;
                                                _logger.LogDebug ( "Document reader hotkey registered: {Hotkey}", FormatHotkey ( modifiers, key ) );
                                                return true;
                                        }
                                        else
                                        {
                                                var formattedHotkey = FormatHotkey ( modifiers, key );
                                                _logger.LogWarning ( "Failed to register document reader hotkey: {Hotkey}", formattedHotkey );
                                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.HotkeyServiceWarning,
                                                        $"Failed to register document reader hotkey: {formattedHotkey}. It might already be in use by another application." );
                                        }
                                }
                                catch ( Exception ex )
                                {
                                        var formattedHotkey = FormatHotkey ( modifiers, key );
                                        _logger.LogError ( ex, "Failed to register document reader hotkey: {Hotkey}", formattedHotkey );
                                        _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                                $"Failed to register document reader hotkey: {formattedHotkey}.", ex );
                                }
                        }

                        return false;
                }

                public void UnregisterHotkey ( )
                {
                        lock ( _lock )
                        {
                                UnregisterHotkeyInternal ( );
                        }
                }

                public void Dispose ( )
                {
                        _source?.RemoveHook ( WndProc );
                        UnregisterHotkey ( );
                        _logger.LogInformation ( "Document reader hotkey service disposed." );
                }

                private static uint ConvertModifiers ( ModifierKeys modifiers )
                {
                        uint result = 0;

                        if ( ( modifiers & ModifierKeys.Alt ) == ModifierKeys.Alt )
                                result |= HotkeyModifierKeys.Alt;

                        if ( ( modifiers & ModifierKeys.Control ) == ModifierKeys.Control )
                                result |= HotkeyModifierKeys.Control;

                        if ( ( modifiers & ModifierKeys.Shift ) == ModifierKeys.Shift )
                                result |= HotkeyModifierKeys.Shift;

                        if ( ( modifiers & ModifierKeys.Windows ) == ModifierKeys.Windows )
                                result |= HotkeyModifierKeys.Win;

                        return result;
                }

                private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
                {
                        if ( msg == WindowsMessages.Hotkey )
                        {
                                _logger.LogDebug ( "Document reader hotkey pressed." );
                                var handler = HotkeyPressed;
                                if ( handler != null )
                                {
                                        _dispatcherInvoker ( handler );
                                }
                                handled = true;
                        }

                        return IntPtr.Zero;
                }

                private void UnregisterHotkeyInternal ( )
                {
                        if ( !_currentHotkeyId.HasValue )
                                return;

                        var hotkeyId = _currentHotkeyId.Value;
                        if ( UnregisterHotKey ( _windowHandle, hotkeyId ) )
                        {
                                _logger.LogDebug ( "Document reader hotkey unregistered with Id: {HotkeyId}", hotkeyId );
                        }
                        else
                        {
                                _logger.LogWarning ( "Failed to unregister document reader hotkey with Id: {HotkeyId}", hotkeyId );
                        }

                        _currentHotkeyId = null;
                }

                private static string FormatHotkey ( ModifierKeys modifiers, Key key )
                {
                        var parts = new List<string> ( );

                        if ( ( modifiers & ModifierKeys.Control ) == ModifierKeys.Control )
                                parts.Add ( "Ctrl" );

                        if ( ( modifiers & ModifierKeys.Shift ) == ModifierKeys.Shift )
                                parts.Add ( "Shift" );

                        if ( ( modifiers & ModifierKeys.Alt ) == ModifierKeys.Alt )
                                parts.Add ( "Alt" );

                        if ( ( modifiers & ModifierKeys.Windows ) == ModifierKeys.Windows )
                                parts.Add ( "Win" );

                        parts.Add ( key.ToString ( ) );
                        return string.Join ( "+", parts );
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
        }

}
