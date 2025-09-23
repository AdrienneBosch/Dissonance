using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
                        _viewModel.OnWindowClosing ( );
                        base.OnClosing ( e );
                }

                private void OnSourceInitialized ( object? sender, EventArgs e )
                {
                        RestoreWindowPlacement ( );
                        _isWindowPlacementInitialized = true;
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
        }
}
