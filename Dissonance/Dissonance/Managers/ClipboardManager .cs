using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;

using Dissonance.Infrastructure.Constants;
using Dissonance.Services.ClipboardService;

using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
        public class ClipboardManager : IDisposable
        {
                private readonly IClipboardService _clipboardService;
                private readonly ILogger<ClipboardManager> _logger;
                private HwndSource? _hwndSource;
                private bool _autoReadEnabled;
                private bool _isListenerRegistered;
                private bool _disposed;

                public ClipboardManager ( IClipboardService clipboardService, ILogger<ClipboardManager> logger )
                {
                        _clipboardService = clipboardService ?? throw new ArgumentNullException ( nameof ( clipboardService ) );
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                }

                public event EventHandler<string>? ClipboardTextReady;

                public string? GetValidatedClipboardText ( )
                {
                        var text = _clipboardService.GetClipboardText();
                        var sanitized = SanitizeClipboardText ( text );
                        if ( string.IsNullOrEmpty ( sanitized ) )
                        {
                                _logger.LogWarning ( "Clipboard is empty or contains invalid text." );
                                return null;
                        }

                        return sanitized;
                }

                public void Initialize ( MainWindow mainWindow )
                {
                        if ( mainWindow == null )
                                throw new ArgumentNullException ( nameof ( mainWindow ) );

                        mainWindow.SourceInitialized += OnMainWindowSourceInitialized;
                        mainWindow.Closed += OnMainWindowClosed;

                        if ( mainWindow.IsLoaded )
                        {
                                AttachToWindow ( mainWindow );
                        }

                        _logger.LogInformation ( "ClipboardManager initialized." );
                }

                public void SetAutoReadClipboard ( bool enabled )
                {
                        if ( _autoReadEnabled == enabled )
                                return;

                        _autoReadEnabled = enabled;
                        UpdateClipboardListener ( );
                }

                public void Dispose ( )
                {
                        if ( _disposed )
                                return;

                        _disposed = true;

                        if ( _hwndSource != null )
                        {
                                if ( _isListenerRegistered )
                                {
                                        RemoveClipboardListener ( );
                                }

                                _hwndSource.RemoveHook ( WndProc );
                                _hwndSource = null;
                        }
                }

                private void OnMainWindowSourceInitialized ( object? sender, EventArgs e )
                {
                        if ( sender is not Window window )
                                return;

                        AttachToWindow ( window );
                }

                private void OnMainWindowClosed ( object? sender, EventArgs e )
                {
                        if ( _hwndSource != null )
                        {
                                if ( _isListenerRegistered )
                                        RemoveClipboardListener ( );

                                _hwndSource.RemoveHook ( WndProc );
                                _hwndSource = null;
                        }
                }

                private void AttachToWindow ( Window window )
                {
                        var source = PresentationSource.FromVisual ( window ) as HwndSource;
                        if ( source == null )
                        {
                                _logger.LogWarning ( "Unable to attach clipboard listener. Window handle not available." );
                                return;
                        }

                        if ( _hwndSource != null && _hwndSource.Handle == source.Handle )
                        {
                                UpdateClipboardListener ( );
                                return;
                        }

                        if ( _hwndSource != null )
                        {
                                _hwndSource.RemoveHook ( WndProc );
                                if ( _isListenerRegistered )
                                {
                                        RemoveClipboardListener ( );
                                }
                        }

                        _hwndSource = source;
                        _hwndSource.AddHook ( WndProc );
                        UpdateClipboardListener ( );
                }

                private void UpdateClipboardListener ( )
                {
                        if ( _hwndSource == null )
                        {
                                _logger.LogDebug ( "Clipboard listener update deferred until window handle is available." );
                                return;
                        }

                        if ( _autoReadEnabled && !_isListenerRegistered )
                        {
                                if ( AddClipboardFormatListener ( _hwndSource.Handle ) )
                                {
                                        _isListenerRegistered = true;
                                        _logger.LogInformation ( "Clipboard listener registered." );
                                }
                                else
                                {
                                        _logger.LogWarning ( "Failed to register clipboard listener." );
                                }
                        }
                        else if ( !_autoReadEnabled && _isListenerRegistered )
                        {
                                RemoveClipboardListener ( );
                        }
                }

                private void RemoveClipboardListener ( )
                {
                        if ( _hwndSource == null )
                                return;

                        if ( RemoveClipboardFormatListener ( _hwndSource.Handle ) )
                        {
                                _logger.LogInformation ( "Clipboard listener unregistered." );
                        }
                        else
                        {
                                _logger.LogWarning ( "Failed to unregister clipboard listener." );
                        }

                        _isListenerRegistered = false;
                }

                private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
                {
                        if ( msg == WindowsMessages.ClipboardUpdate && _autoReadEnabled )
                        {
                                try
                                {
                                        var clipboardText = GetValidatedClipboardText ( );
                                        if ( !string.IsNullOrEmpty ( clipboardText ) )
                                        {
                                                ClipboardTextReady?.Invoke ( this, clipboardText );
                                        }
                                }
                                catch ( Exception ex )
                                {
                                        _logger.LogError ( ex, "Error processing clipboard update." );
                                }
                        }

                        return IntPtr.Zero;
                }

                private static string? SanitizeClipboardText ( string? text )
                {
                        if ( string.IsNullOrWhiteSpace ( text ) )
                                return null;

                        var sanitized = Regex.Replace ( text, "\\s+", " " ).Trim ( );
                        return string.IsNullOrEmpty ( sanitized ) ? null : sanitized;
                }

                [DllImport ( "user32.dll", SetLastError = true )]
                [return: MarshalAs ( UnmanagedType.Bool )]
                private static extern bool AddClipboardFormatListener ( IntPtr hwnd );

                [DllImport ( "user32.dll", SetLastError = true )]
                [return: MarshalAs ( UnmanagedType.Bool )]
                private static extern bool RemoveClipboardFormatListener ( IntPtr hwnd );
        }
}
