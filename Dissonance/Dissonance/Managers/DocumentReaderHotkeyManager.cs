using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Dissonance.Services.DocumentReaderHotkeyService;
using Dissonance.ViewModels;

using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
        public class DocumentReaderHotkeyManager : IDisposable
        {
                private readonly IDocumentReaderHotkeyService _hotkeyService;
                private readonly DocumentReaderViewModel _documentReaderViewModel;
                private readonly ILogger<DocumentReaderHotkeyManager> _logger;
                private bool _isInitialized;
                private bool _disposed;

                public DocumentReaderHotkeyManager ( IDocumentReaderHotkeyService hotkeyService,
                        DocumentReaderViewModel documentReaderViewModel,
                        ILogger<DocumentReaderHotkeyManager> logger )
                {
                        _hotkeyService = hotkeyService ?? throw new ArgumentNullException ( nameof ( hotkeyService ) );
                        _documentReaderViewModel = documentReaderViewModel ?? throw new ArgumentNullException ( nameof ( documentReaderViewModel ) );
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );

                        _documentReaderViewModel.PropertyChanged += OnDocumentReaderPropertyChanged;
                }

                public void Initialize ( MainWindow mainWindow )
                {
                        if ( mainWindow == null )
                                throw new ArgumentNullException ( nameof ( mainWindow ) );

                        if ( _isInitialized )
                        {
                                _logger.LogDebug ( "Document reader hotkey manager already initialized." );
                                return;
                        }

                        _hotkeyService.Initialize ( mainWindow );
                        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
                        _isInitialized = true;
                        UpdateHotkeyRegistration ( );
                        _logger.LogInformation ( "Document reader hotkey manager initialized." );
                }

                public void Dispose ( )
                {
                        if ( _disposed )
                                return;

                        _documentReaderViewModel.PropertyChanged -= OnDocumentReaderPropertyChanged;

                        if ( _isInitialized )
                        {
                                _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
                                _hotkeyService.UnregisterHotkey ( );
                        }

                        _hotkeyService.Dispose ( );
                        _disposed = true;
                        _logger.LogInformation ( "Document reader hotkey manager disposed." );
                }

                private void OnDocumentReaderPropertyChanged ( object? sender, PropertyChangedEventArgs e )
                {
                        if ( !_isInitialized )
                                return;

                        if ( e.PropertyName == nameof ( DocumentReaderViewModel.PlaybackHotkeyKey )
                                || e.PropertyName == nameof ( DocumentReaderViewModel.PlaybackHotkeyModifiers ) )
                        {
                                UpdateHotkeyRegistration ( );
                        }
                }

                private void UpdateHotkeyRegistration ( )
                {
                        if ( !_isInitialized )
                                return;

                        var key = _documentReaderViewModel.PlaybackHotkeyKey;
                        if ( key == Key.None )
                        {
                                _hotkeyService.UnregisterHotkey ( );
                                _logger.LogInformation ( "Document reader global hotkey cleared." );
                                return;
                        }

                        var modifiers = _documentReaderViewModel.PlaybackHotkeyModifiers;
                        if ( _hotkeyService.RegisterHotkey ( modifiers, key ) )
                        {
                                _logger.LogInformation ( "Document reader global hotkey updated to {Hotkey}", FormatHotkey ( modifiers, key ) );
                        }
                }

                private void OnHotkeyPressed ( )
                {
                        var mainWindow = Application.Current?.MainWindow;
                        if ( mainWindow != null && mainWindow.IsActive )
                        {
                                _logger.LogDebug ( "Ignoring global document reader hotkey because the main window is active." );
                                return;
                        }

                        var command = _documentReaderViewModel.PlaybackHotkeyCommand;
                        if ( command?.CanExecute ( null ) == true )
                        {
                                command.Execute ( null );
                                _logger.LogInformation ( "Document reader hotkey command executed globally." );
                        }
                        else
                        {
                                _logger.LogDebug ( "Document reader hotkey command execution skipped; command unavailable." );
                        }
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
        }
}
