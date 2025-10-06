using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Dissonance.Services.MessageService;

using Microsoft.Extensions.Logging;

using MessageBoxTitles = Dissonance.Infrastructure.Constants.MessageBoxTitles;

namespace Dissonance.Services.DocumentReaderHotkeyService
{
        internal class DocumentReaderHotkeyService : IDocumentReaderHotkeyService
        {
                private const int WhKeyboardLl = 13;
                private const int WmKeydown = 0x0100;
                private const int WmSyskeydown = 0x0104;
                private const int WmKeyup = 0x0101;
                private const int WmSyskeyup = 0x0105;
                private const int VkShift = 0x10;
                private const int VkControl = 0x11;
                private const int VkMenu = 0x12;
                private const int VkLshift = 0xA0;
                private const int VkRshift = 0xA1;
                private const int VkLcontrol = 0xA2;
                private const int VkRcontrol = 0xA3;
                private const int VkLmenu = 0xA4;
                private const int VkRmenu = 0xA5;
                private const int VkLwin = 0x5B;
                private const int VkRwin = 0x5C;

                private readonly object _lock = new ( );
                private readonly ILogger<DocumentReaderHotkeyService> _logger;
                private readonly IMessageService _messageService;
                private readonly Action<Action> _dispatcherInvoker;
                private ModifierKeys _registeredModifiers = ModifierKeys.None;
                private Key _registeredKey = Key.None;
                private bool _isInitialized;
                private IntPtr _hookHandle;
                private LowLevelKeyboardProc? _keyboardProc;
                private int _controlCount;
                private int _shiftCount;
                private int _altCount;
                private int _winCount;

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

                public void Initialize ( Window mainWindow )
                {
                        if ( mainWindow == null )
                                throw new ArgumentNullException ( nameof ( mainWindow ), "MainWindow cannot be null." );

                        lock ( _lock )
                        {
                                if ( _isInitialized )
                                {
                                        _logger.LogDebug ( "Document reader hotkey service already initialized." );
                                        return;
                                }

                                try
                                {
                                        EnsureKeyboardHookInstalled ( );
                                        _isInitialized = true;
                                        _logger.LogInformation ( "Document reader hotkey service initialized with global keyboard hook." );
                                }
                                catch ( Win32Exception ex )
                                {
                                        _logger.LogError ( ex, "Failed to initialize document reader hotkey service." );
                                        _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                                "Failed to initialize system-wide document reader hotkey support.", ex );
                                        throw;
                                }
                                catch ( Exception ex )
                                {
                                        _logger.LogError ( ex, "Unexpected error while initializing document reader hotkey service." );
                                        _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                                "Failed to initialize system-wide document reader hotkey support due to an unexpected error.", ex );
                                        throw;
                                }
                        }
                }

                public bool RegisterHotkey ( ModifierKeys modifiers, Key key )
                {
                        if ( key == Key.None )
                                throw new ArgumentException ( "Key cannot be Key.None when registering a hotkey.", nameof ( key ) );

                        lock ( _lock )
                        {
                                if ( !_isInitialized )
                                        throw new InvalidOperationException ( "The hotkey service must be initialized before registering a hotkey." );

                                try
                                {
                                        EnsureKeyboardHookInstalled ( );
                                        _registeredModifiers = modifiers;
                                        _registeredKey = key;
                                        _logger.LogDebug ( "Document reader hotkey registered via keyboard hook: {Hotkey}", FormatHotkey ( modifiers, key ) );
                                        return true;
                                }
                                catch ( Win32Exception ex )
                                {
                                        _logger.LogError ( ex, "Failed to register document reader hotkey using keyboard hook." );
                                        _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                                $"Failed to register document reader hotkey: {FormatHotkey ( modifiers, key )}.", ex );
                                }
                                catch ( Exception ex )
                                {
                                        _logger.LogError ( ex, "Unexpected error while registering document reader hotkey." );
                                        _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.HotkeyServiceError,
                                                $"Failed to register document reader hotkey: {FormatHotkey ( modifiers, key )}.", ex );
                                }
                        }

                        return false;
                }

                public void UnregisterHotkey ( )
                {
                        lock ( _lock )
                        {
                                _registeredKey = Key.None;
                                _registeredModifiers = ModifierKeys.None;
                                _controlCount = 0;
                                _shiftCount = 0;
                                _altCount = 0;
                                _winCount = 0;
                                _logger.LogDebug ( "Document reader hotkey cleared." );
                        }
                }

                public void Dispose ( )
                {
                        lock ( _lock )
                        {
                                _registeredKey = Key.None;
                                _registeredModifiers = ModifierKeys.None;

                                if ( _hookHandle != IntPtr.Zero )
                                {
                                        UnhookWindowsHookEx ( _hookHandle );
                                        _hookHandle = IntPtr.Zero;
                                        _logger.LogInformation ( "Document reader hotkey service disposed and keyboard hook removed." );
                                }
                        }
                }

                private void EnsureKeyboardHookInstalled ( )
                {
                        if ( _hookHandle != IntPtr.Zero )
                                return;

                        _keyboardProc ??= HookCallback;

                        using var currentProcess = Process.GetCurrentProcess ( );
                        using var currentModule = currentProcess.MainModule ?? throw new InvalidOperationException ( "Failed to retrieve process module for keyboard hook." );
                        var moduleHandle = GetModuleHandle ( currentModule.ModuleName );
                        if ( moduleHandle == IntPtr.Zero )
                        {
                                throw new Win32Exception ( Marshal.GetLastWin32Error ( ), "Failed to get module handle for keyboard hook." );
                        }

                        _hookHandle = SetWindowsHookEx ( WhKeyboardLl, _keyboardProc, moduleHandle, 0 );
                        if ( _hookHandle == IntPtr.Zero )
                        {
                                throw new Win32Exception ( Marshal.GetLastWin32Error ( ), "Failed to install global keyboard hook." );
                        }
                }

                private IntPtr HookCallback ( int nCode, IntPtr wParam, IntPtr lParam )
                {
                        if ( nCode >= 0 && _registeredKey != Key.None )
                        {
                                var message = wParam.ToInt32 ( );
                                var isKeyDown = message == WmKeydown || message == WmSyskeydown;
                                var isKeyUp = message == WmKeyup || message == WmSyskeyup;

                                if ( isKeyDown || isKeyUp )
                                {
                                        if ( lParam != IntPtr.Zero )
                                        {
                                                var hookStruct = Marshal.PtrToStructure<KbdLlHookStruct> ( lParam );
                                                UpdateModifierState ( hookStruct.VkCode, isKeyDown );

                                                if ( isKeyDown && IsHotkeyMatch ( hookStruct.VkCode ) )
                                                {
                                                        _logger.LogDebug ( "Document reader global hotkey detected via keyboard hook." );
                                                        var handler = HotkeyPressed;
                                                        if ( handler != null )
                                                        {
                                                                _dispatcherInvoker ( handler );
                                                        }
                                                }
                                        }
                                }
                        }

                        return CallNextHookEx ( _hookHandle, nCode, wParam, lParam );
                }

                private void UpdateModifierState ( uint virtualKey, bool isKeyDown )
                {
                        if ( IsControlKey ( virtualKey ) )
                        {
                                UpdateModifierCounter ( ref _controlCount, isKeyDown );
                                return;
                        }

                        if ( IsShiftKey ( virtualKey ) )
                        {
                                UpdateModifierCounter ( ref _shiftCount, isKeyDown );
                                return;
                        }

                        if ( IsAltKey ( virtualKey ) )
                        {
                                UpdateModifierCounter ( ref _altCount, isKeyDown );
                                return;
                        }

                        if ( IsWinKey ( virtualKey ) )
                        {
                                UpdateModifierCounter ( ref _winCount, isKeyDown );
                        }
                }

                private static void UpdateModifierCounter ( ref int counter, bool isKeyDown )
                {
                        if ( isKeyDown )
                        {
                                counter++;
                        }
                        else if ( counter > 0 )
                        {
                                counter--;
                        }
                }

                private bool IsHotkeyMatch ( uint virtualKey )
                {
                        if ( _registeredKey == Key.None )
                                return false;

                        var expectedVirtualKey = (uint)KeyInterop.VirtualKeyFromKey ( _registeredKey );
                        if ( virtualKey != expectedVirtualKey )
                                return false;

                        var currentModifiers = GetActiveModifiers ( );
                        return currentModifiers == _registeredModifiers;
                }

                private ModifierKeys GetActiveModifiers ( )
                {
                        var modifiers = ModifierKeys.None;

                        if ( _controlCount > 0 )
                                modifiers |= ModifierKeys.Control;

                        if ( _shiftCount > 0 )
                                modifiers |= ModifierKeys.Shift;

                        if ( _altCount > 0 )
                                modifiers |= ModifierKeys.Alt;

                        if ( _winCount > 0 )
                                modifiers |= ModifierKeys.Windows;

                        return modifiers;
                }

                private static bool IsControlKey ( uint virtualKey )
                {
                        switch ( virtualKey )
                        {
                                case VkControl:
                                case VkLcontrol:
                                case VkRcontrol:
                                        return true;
                                default:
                                        return false;
                        }
                }

                private static bool IsShiftKey ( uint virtualKey )
                {
                        switch ( virtualKey )
                        {
                                case VkShift:
                                case VkLshift:
                                case VkRshift:
                                        return true;
                                default:
                                        return false;
                        }
                }

                private static bool IsAltKey ( uint virtualKey )
                {
                        switch ( virtualKey )
                        {
                                case VkMenu:
                                case VkLmenu:
                                case VkRmenu:
                                        return true;
                                default:
                                        return false;
                        }
                }

                private static bool IsWinKey ( uint virtualKey )
                {
                        return virtualKey == VkLwin || virtualKey == VkRwin;
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

                [StructLayout ( LayoutKind.Sequential )]
                private struct KbdLlHookStruct
                {
                        public uint VkCode;
                        public uint ScanCode;
                        public uint Flags;
                        public uint Time;
                        public IntPtr DwExtraInfo;
                }

                private delegate IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam );

                [DllImport ( "user32.dll", SetLastError = true )]
                private static extern IntPtr SetWindowsHookEx ( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

                [DllImport ( "user32.dll", SetLastError = true )]
                private static extern bool UnhookWindowsHookEx ( IntPtr hhk );

                [DllImport ( "user32.dll" )]
                private static extern IntPtr CallNextHookEx ( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );

                [DllImport ( "kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
                private static extern IntPtr GetModuleHandle ( string lpModuleName );
        }
}
