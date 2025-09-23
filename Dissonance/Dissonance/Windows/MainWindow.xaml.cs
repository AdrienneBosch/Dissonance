using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Dissonance.ViewModels;

namespace Dissonance
{
        public partial class MainWindow : Window
        {
                private readonly MainWindowViewModel _viewModel;
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

                public MainWindow ( MainWindowViewModel viewModel )
                {
                        _viewModel = viewModel ?? throw new ArgumentNullException ( nameof ( viewModel ) );
                        InitializeComponent ( );
                        DataContext = _viewModel;
                }

                protected override void OnClosing ( CancelEventArgs e )
                {
                        _viewModel.OnWindowClosing ( );
                        base.OnClosing ( e );
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

                private void SettingsMenuPopup_Opened ( object? sender, EventArgs e )
                {
                        Dispatcher.BeginInvoke ( new Action ( ( ) => SaveSettingsMenuButton.Focus ( ) ), DispatcherPriority.Input );
                }

                private void SettingsMenuPopup_Closed ( object? sender, EventArgs e )
                {
                        SettingsButton.Focus ( );
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

                        if ( hotkeyParts.Count == 0 )
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
        }
}
