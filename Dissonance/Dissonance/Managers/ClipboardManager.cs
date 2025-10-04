using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

using Dissonance.Infrastructure.Constants;
using Dissonance.Services.ClipboardService;
using Dissonance.Services.StatusAnnouncements;

using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
        public class ClipboardManager : IDisposable
        {
                private readonly IClipboardService _clipboardService;
                private readonly ILogger<ClipboardManager> _logger;
                private readonly IStatusAnnouncementService _statusAnnouncementService;
                private HwndSource? _hwndSource;
                private bool _autoReadEnabled;
                private bool _isListenerRegistered;
                private bool _disposed;
                private bool _suppressNextAutoRead;
                private DateTime? _suppressAutoReadExpiryUtc;

                public ClipboardManager ( IClipboardService clipboardService, ILogger<ClipboardManager> logger, IStatusAnnouncementService statusAnnouncementService )
                {
                        _clipboardService = clipboardService ?? throw new ArgumentNullException ( nameof ( clipboardService ) );
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                        _statusAnnouncementService = statusAnnouncementService ?? throw new ArgumentNullException ( nameof ( statusAnnouncementService ) );
                }

                public event EventHandler<string>? ClipboardTextReady;

                public string? GetValidatedClipboardText ( )
                {
                        var text = _clipboardService.GetClipboardText ( );
                        var sanitized = SanitizeClipboardText ( text );
                        if ( string.IsNullOrEmpty ( sanitized ) )
                        {
                                _logger.LogWarning ( "Clipboard is empty or contains invalid text." );
                                _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardEmpty", "Clipboard was empty or didn't contain readable text.", StatusSeverity.Warning );
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

                public string? CopySelectionAndGetValidatedText ( )
                {
                        var suppressionApplied = false;
                        var copySucceeded = false;

                        try
                        {
                                if ( _autoReadEnabled )
                                {
                                        _suppressNextAutoRead = true;
                                        _suppressAutoReadExpiryUtc = DateTime.UtcNow.AddSeconds ( 2 );
                                        suppressionApplied = true;
                                }

                                SimulateCopyShortcut ( );

                                string? sanitized = null;
                                var textDetected = false;

                                for ( var attempt = 0; attempt < 10; attempt++ )
                                {
                                        Thread.Sleep ( 25 );

                                        if ( !_clipboardService.IsTextAvailable ( ) )
                                                continue;

                                        textDetected = true;
                                        var text = _clipboardService.GetClipboardText ( );
                                        sanitized = SanitizeClipboardText ( text );
                                        if ( !string.IsNullOrEmpty ( sanitized ) )
                                        {
                                                copySucceeded = true;
                                                return sanitized;
                                        }
                                }

                                if ( !textDetected )
                                {
                                        _logger.LogWarning ( "No text selection was copied when the hotkey was pressed." );
                                        _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardSelectionMissing", "Nothing was copied. Try selecting text before using the shortcut.", StatusSeverity.Warning );
                                }
                                else
                                {
                                        _logger.LogWarning ( "Copied selection did not contain readable text." );
                                        _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardUnreadable", "Copied text couldn't be read.", StatusSeverity.Warning );
                                }

                                return null;
                        }
                        catch ( Exception ex )
                        {
                                _logger.LogError ( ex, "Failed to copy the current selection using the clipboard hotkey." );
                                _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardCopyError", "We couldn't copy the selected text.", StatusSeverity.Error );
                                return null;
                        }
                        finally
                        {
                                if ( suppressionApplied && !copySucceeded )
                                {
                                        _suppressNextAutoRead = false;
                                        _suppressAutoReadExpiryUtc = null;
                                }
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
                                _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardListenerUnavailable", "Clipboard listener unavailable. Automatic reading may not work.", StatusSeverity.Warning );
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
                                        _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardListenerRegistrationFailed", "Couldn't start listening for clipboard changes.", StatusSeverity.Error );
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
                                _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardListenerUnregisterFailed", "Couldn't stop listening for clipboard changes.", StatusSeverity.Warning );
                        }

                        _isListenerRegistered = false;
                }

                private IntPtr WndProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
                {
                        if ( msg == WindowsMessages.ClipboardUpdate && _autoReadEnabled )
                        {
                                if ( _suppressNextAutoRead )
                                {
                                        if ( !_suppressAutoReadExpiryUtc.HasValue || DateTime.UtcNow <= _suppressAutoReadExpiryUtc.Value )
                                        {
                                                _suppressNextAutoRead = false;
                                                _suppressAutoReadExpiryUtc = null;
                                                _logger.LogDebug ( "Suppressed clipboard update triggered by manual hotkey copy." );
                                                return IntPtr.Zero;
                                        }

                                        _suppressNextAutoRead = false;
                                        _suppressAutoReadExpiryUtc = null;
                                        _logger.LogDebug ( "Manual copy suppression expired before a clipboard update message arrived." );
                                }

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
                                        _statusAnnouncementService.AnnounceFromResource ( "StatusMessageClipboardUpdateError", "We couldn't process the latest clipboard change.", StatusSeverity.Error );
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

                private static void SimulateCopyShortcut ( )
                {
                        keybd_event ( VK_CONTROL, 0, 0, UIntPtr.Zero );
                        keybd_event ( VK_C, 0, 0, UIntPtr.Zero );
                        keybd_event ( VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero );
                        keybd_event ( VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero );
                }

                [DllImport ( "user32.dll", SetLastError = true )]
                [return: MarshalAs ( UnmanagedType.Bool )]
                private static extern bool AddClipboardFormatListener ( IntPtr hwnd );

                [DllImport ( "user32.dll", SetLastError = true )]
                [return: MarshalAs ( UnmanagedType.Bool )]
                private static extern bool RemoveClipboardFormatListener ( IntPtr hwnd );

                [DllImport ( "user32.dll", SetLastError = false )]
                private static extern void keybd_event ( byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo );

                private const uint KEYEVENTF_KEYUP = 0x0002;
                private const byte VK_CONTROL = 0x11;
                private const byte VK_C = 0x43;
        }
}
