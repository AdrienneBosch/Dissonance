using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;

using Dissonance.Services.SettingsService;
using Dissonance.ViewModels;

namespace Dissonance
{
        public partial class MainWindow : Window
        {
                private readonly MainWindowViewModel _viewModel;
                private readonly ISettingsService _settingsService;
                private bool _isWindowPlacementInitialized;
                private WindowState _lastNonMinimizedWindowState = WindowState.Normal;
                private bool _isWindowPlacementDirty;
                private static readonly HashSet<Key> ModifierKeySet = new HashSet<Key>
                {
                        Key.LeftCtrl,
                        Key.RightCtrl,
                        Key.LeftShift,
                        Key.RightShift,
                        Key.LeftAlt,
                        Key.RightAlt,
                        Key.LWin,
                        Key.RWin,
                };

                public MainWindow ( MainWindowViewModel viewModel, ISettingsService settingsService )
                {
                        _viewModel = viewModel ?? throw new ArgumentNullException ( nameof ( viewModel ) );
                        _settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
                        InitializeComponent ( );
                        DataContext = _viewModel;

                        SourceInitialized += OnSourceInitialized;
                        LocationChanged += OnWindowLocationChanged;
                        SizeChanged += OnWindowSizeChanged;
                        StateChanged += OnWindowStateChanged;
                }

                protected override void OnClosing ( CancelEventArgs e )
                {
                        PersistWindowPlacement ( );
                        if ( _isWindowPlacementDirty )
                        {
                                _settingsService.SaveCurrentSettings ( );
                                _isWindowPlacementDirty = false;
                        }
                        _viewModel.OnWindowClosing ( );
                        base.OnClosing ( e );
                }

                private void OnSourceInitialized ( object? sender, EventArgs e )
                {
                        if ( PresentationSource.FromVisual ( this ) is HwndSource hwndSource )
                                hwndSource.AddHook ( WindowProc );

                        RestoreWindowPlacement ( );
                        _isWindowPlacementInitialized = true;
                }

                private IntPtr WindowProc ( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
                {
                        if ( msg == WM_GETMINMAXINFO )
                        {
                                WmGetMinMaxInfo ( hwnd, lParam );
                                handled = true;
                        }

                        return IntPtr.Zero;
                }

                private void RestoreWindowPlacement ( )
                {
                        var settings = _settingsService.GetCurrentSettings ( );

                        if ( settings.WindowWidth.HasValue && settings.WindowWidth.Value > 0 )
                                Width = settings.WindowWidth.Value;

                        if ( settings.WindowHeight.HasValue && settings.WindowHeight.Value > 0 )
                                Height = settings.WindowHeight.Value;

                        if ( settings.WindowLeft.HasValue && settings.WindowTop.HasValue )
                        {
                                WindowStartupLocation = WindowStartupLocation.Manual;
                                Left = settings.WindowLeft.Value;
                                Top = settings.WindowTop.Value;
                        }

                        _lastNonMinimizedWindowState = settings.IsWindowMaximized ? WindowState.Maximized : WindowState.Normal;

                        if ( settings.IsWindowMaximized )
                                WindowState = WindowState.Maximized;
                }

                private void OnWindowLocationChanged ( object? sender, EventArgs e )
                {
                        if ( WindowState != WindowState.Normal )
                                return;

                        PersistWindowPlacement ( );
                }

                private void OnWindowSizeChanged ( object sender, SizeChangedEventArgs e )
                {
                        if ( WindowState != WindowState.Normal )
                                return;

                        PersistWindowPlacement ( );
                }

                private void OnWindowStateChanged ( object? sender, EventArgs e )
                {
                        if ( WindowState != WindowState.Minimized )
                                _lastNonMinimizedWindowState = WindowState;

                        PersistWindowPlacement ( );
                }

                private void PersistWindowPlacement ( )
                {
                        if ( !_isWindowPlacementInitialized )
                                return;

                        var settings = _settingsService.GetCurrentSettings ( );

                        switch ( WindowState )
                        {
                                case WindowState.Normal:
                                        settings.WindowWidth = ActualWidth;
                                        settings.WindowHeight = ActualHeight;
                                        settings.WindowLeft = Left;
                                        settings.WindowTop = Top;
                                        settings.IsWindowMaximized = false;
                                        break;
                                case WindowState.Maximized:
                                case WindowState.Minimized:
                                        var bounds = RestoreBounds;
                                        settings.WindowWidth = bounds.Width;
                                        settings.WindowHeight = bounds.Height;
                                        settings.WindowLeft = bounds.Left;
                                        settings.WindowTop = bounds.Top;
                                        settings.IsWindowMaximized = WindowState == WindowState.Maximized || ( WindowState == WindowState.Minimized && _lastNonMinimizedWindowState == WindowState.Maximized );
                                        break;
                        }

                        _isWindowPlacementDirty = true;
                }

                private void MinimizeButton_Click ( object sender, RoutedEventArgs e )
                {
                        WindowState = WindowState.Minimized;
                }

                private void MaximizeButton_Click ( object sender, RoutedEventArgs e )
                {
                        ToggleWindowState ( );
                }

                private void CloseButton_Click ( object sender, RoutedEventArgs e )
                {
                        Close ( );
                }

                private void TitleBar_MouseLeftButtonDown ( object sender, MouseButtonEventArgs e )
                {
                        if ( e.OriginalSource is DependencyObject originalSource && IsWithinWindowControl ( originalSource ) )
                        {
                                return;
                        }

                        if ( e.ClickCount == 2 && ResizeMode != ResizeMode.NoResize )
                        {
                                ToggleWindowState ( );
                                return;
                        }

                        DragMove ( );
                }

                private void ToggleWindowState ( )
                {
                        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }

                private void WmGetMinMaxInfo ( IntPtr hwnd, IntPtr lParam )
                {
                        var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO> ( lParam );
                        var monitorHandle = MonitorFromWindow ( hwnd, MONITOR_DEFAULTTONEAREST );

                        if ( monitorHandle != IntPtr.Zero )
                        {
                                var monitorInfo = new MONITORINFO
                                {
                                        cbSize = Marshal.SizeOf<MONITORINFO> ( ),
                                };

                                if ( GetMonitorInfo ( monitorHandle, ref monitorInfo ) )
                                {
                                        var workArea = monitorInfo.rcWork;
                                        var monitorArea = monitorInfo.rcMonitor;

                                        minMaxInfo.ptMaxPosition.X = Math.Abs ( workArea.Left - monitorArea.Left );
                                        minMaxInfo.ptMaxPosition.Y = Math.Abs ( workArea.Top - monitorArea.Top );
                                        minMaxInfo.ptMaxSize.X = workArea.Right - workArea.Left;
                                        minMaxInfo.ptMaxSize.Y = workArea.Bottom - workArea.Top;
                                }
                        }

                        Marshal.StructureToPtr ( minMaxInfo, lParam, true );
                }

                private static bool IsWithinWindowControl ( DependencyObject source )
                {
                        DependencyObject? current = source;

                        while ( current != null )
                        {
                                if ( current is Button )
                                {
                                        return true;
                                }

                                current = VisualTreeHelper.GetParent ( current );
                        }

                        return false;
                }

                private void SettingsButton_Click ( object sender, RoutedEventArgs e )
                {
                        SettingsMenuPopup.IsOpen = !SettingsMenuPopup.IsOpen;
                }

                private void SettingsMenuItem_Click ( object sender, RoutedEventArgs e )
                {
                        SettingsMenuPopup.IsOpen = false;
                }

                private void SettingsMenuAutoSave_Click ( object sender, RoutedEventArgs e )
                {
                        SettingsMenuPopup.IsOpen = false;
                }

                private void SettingsMenuPopup_Opened ( object? sender, EventArgs e )
                {
                        Dispatcher.BeginInvoke ( new Action ( ( ) => SaveSettingsAsDefaultMenuButton.Focus ( ) ), DispatcherPriority.Input );
                }

                private void SettingsMenuPopup_Closed ( object? sender, EventArgs e )
                {
                        SettingsButton.Focus ( );
                }

                private void NavigationMenuPopup_Opened ( object? sender, EventArgs e )
                {
                        Dispatcher.BeginInvoke ( new Action ( ( ) =>
                        {
                                if ( NavigationHomeButton.IsVisible )
                                        NavigationHomeButton.Focus ( );
                                else
                                        NavigationListBox.Focus ( );
                        } ), DispatcherPriority.Input );
                }

                private void NavigationMenuPopup_Closed ( object? sender, EventArgs e )
                {
                        _viewModel.IsNavigationMenuOpen = false;
                        NavigationToggleButton.IsChecked = false;
                        NavigationToggleButton.Focus ( );
                }

                private void NavigationListBox_PreviewKeyDown ( object sender, KeyEventArgs e )
                {
                        if ( ( e.Key == Key.Enter || e.Key == Key.Space ) && Keyboard.Modifiers == ModifierKeys.None )
                        {
                                if ( NavigationListBox.SelectedItem is NavigationSectionViewModel section )
                                {
                                        if ( _viewModel.NavigateToSectionCommand?.CanExecute ( section ) == true )
                                        {
                                                _viewModel.NavigateToSectionCommand.Execute ( section );
                                                e.Handled = true;
                                        }
                                }
                        }
                        else if ( e.Key == Key.Escape )
                        {
                                if ( NavigationMenuPopup.IsOpen )
                                {
                                        NavigationMenuPopup.IsOpen = false;
                                        e.Handled = true;
                                }
                        }
                }

                private void ReadClipboardHotkeyTextBox_PreviewKeyDown ( object sender, KeyEventArgs e )
                {
                        var key = e.Key == Key.System ? e.SystemKey : e.Key;

                        if ( key == Key.Tab )
                        {
                                var modifiersState = Keyboard.Modifiers;
                                if ( modifiersState == ModifierKeys.None || modifiersState == ModifierKeys.Shift )
                                {
                                        e.Handled = false;
                                        return;
                                }
                        }

                        e.Handled = true;

                        if ( key == Key.None || IsModifierKey ( key ) )
                        {
                                return;
                        }

                        var modifiers = GetActiveModifiers ( );
                        var hotkeyParts = new List<string> ( modifiers ) { key.ToString ( ) };

                        if ( hotkeyParts.Count < 2 )
                        {
                                return;
                        }

                        _viewModel.HotkeyCombination = string.Join ( "+", hotkeyParts );
                }

                private static bool IsModifierKey ( Key key )
                {
                        return ModifierKeySet.Contains ( key );
                }

                private static List<string> GetActiveModifiers ( )
                {
                        var modifiers = new List<string> ( );
                        var currentModifiers = Keyboard.Modifiers;

                        if ( ( currentModifiers & ModifierKeys.Control ) == ModifierKeys.Control )
                        {
                                modifiers.Add ( "Ctrl" );
                        }

                        if ( ( currentModifiers & ModifierKeys.Shift ) == ModifierKeys.Shift )
                        {
                                modifiers.Add ( "Shift" );
                        }

                        if ( ( currentModifiers & ModifierKeys.Alt ) == ModifierKeys.Alt )
                        {
                                modifiers.Add ( "Alt" );
                        }

                        if ( ( currentModifiers & ModifierKeys.Windows ) == ModifierKeys.Windows )
                        {
                                modifiers.Add ( "Win" );
                        }

                        return modifiers;
                }

                private void VoiceVolumeSlider_KeyDown ( object sender, KeyEventArgs e )
                {
                        if ( sender is not Slider slider )
                        {
                                return;
                        }

                        if ( Keyboard.Modifiers != ModifierKeys.None )
                        {
                                return;
                        }

                        double change;

                        switch ( e.Key )
                        {
                                case Key.Left:
                                case Key.Down:
                                        change = -slider.SmallChange;
                                        break;
                                case Key.Right:
                                case Key.Up:
                                        change = slider.SmallChange;
                                        break;
                                case Key.PageDown:
                                        change = -slider.LargeChange;
                                        break;
                                case Key.PageUp:
                                        change = slider.LargeChange;
                                        break;
                                case Key.Home:
                                        slider.Value = slider.Minimum;
                                        e.Handled = true;
                                        return;
                                case Key.End:
                                        slider.Value = slider.Maximum;
                                        e.Handled = true;
                                        return;
                                default:
                                        return;
                        }

                        var updatedValue = slider.Value + change;
                        slider.Value = Math.Max ( slider.Minimum, Math.Min ( slider.Maximum, updatedValue ) );
                        e.Handled = true;
                }

                private const int WM_GETMINMAXINFO = 0x0024;
                private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

                [DllImport ( "user32.dll" )]
                private static extern IntPtr MonitorFromWindow ( IntPtr hwnd, int dwFlags );

                [DllImport ( "user32.dll", SetLastError = true )]
                [return: MarshalAs ( UnmanagedType.Bool )]
                private static extern bool GetMonitorInfo ( IntPtr hMonitor, ref MONITORINFO lpmi );

                [StructLayout ( LayoutKind.Sequential )]
                private struct POINT
                {
                        public int X;
                        public int Y;
                }

                [StructLayout ( LayoutKind.Sequential )]
                private struct MINMAXINFO
                {
                        public POINT ptReserved;
                        public POINT ptMaxSize;
                        public POINT ptMaxPosition;
                        public POINT ptMinTrackSize;
                        public POINT ptMaxTrackSize;
                }

                [StructLayout ( LayoutKind.Sequential )]
                private struct MONITORINFO
                {
                        public int cbSize;
                        public RECT rcMonitor;
                        public RECT rcWork;
                        public int dwFlags;
                }

                [StructLayout ( LayoutKind.Sequential )]
                private struct RECT
                {
                        public int Left;
                        public int Top;
                        public int Right;
                        public int Bottom;
                }
        }
}
