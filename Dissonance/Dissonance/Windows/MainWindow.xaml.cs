using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Dissonance.ViewModels;

namespace Dissonance
{
        public partial class MainWindow : Window
        {
                private readonly MainWindowViewModel _viewModel;

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
        }
}
