using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Dissonance.Infrastructure.Commands;
using Dissonance.Windows.Controls;

namespace Dissonance.ViewModels
{
        internal class DissonanceMessageBoxViewModel
        {
                private readonly DissonanceMessageBox _messageBox;

                public DissonanceMessageBoxViewModel ( DissonanceMessageBox messageBox )
                {
                        _messageBox = messageBox;
                        OkCommand = new RelayCommandNoParam ( Ok );
                        CancelCommand = new RelayCommandNoParam ( Cancel );
                }

                public ICommand CancelCommand { get; }

                public string Message { get; set; }

                public ICommand OkCommand { get; }

                public bool ShowCancelButton { get; set; }

                public string Title { get; set; }

                private void Cancel ( )
                {
                        _messageBox.DialogResult = false;
                        _messageBox.Close ( );
                }

                private void Ok ( )
                {
                        _messageBox.DialogResult = true;
                        _messageBox.Close ( );
                }

                public static bool? Show ( string title, string message, bool showCancelButton = false, TimeSpan? autoCloseDelay = null )
                {
                        var messageBox = new DissonanceMessageBox();
                        var viewModel = new DissonanceMessageBoxViewModel(messageBox)
                        {
                                Title = title,
                                Message = message,
                                ShowCancelButton = showCancelButton
                        };

                        messageBox.DataContext = viewModel;

                        var ownerWindow = GetOwnerWindow();
                        if ( ownerWindow != null && !ReferenceEquals ( ownerWindow, messageBox ) )
                        {
                                messageBox.Owner = ownerWindow;
                        }

                        DispatcherTimer autoCloseTimer = null;

                        if ( autoCloseDelay.HasValue )
                        {
                                autoCloseTimer = new DispatcherTimer
                                {
                                        Interval = autoCloseDelay.Value
                                };

                                autoCloseTimer.Tick += ( sender, args ) =>
                                {
                                        autoCloseTimer.Stop ( );

                                        if ( messageBox.IsVisible )
                                        {
                                                messageBox.DialogResult = true;
                                                messageBox.Close ( );
                                        }
                                };

                                messageBox.Loaded += ( sender, args ) => autoCloseTimer.Start ( );
                                messageBox.Closed += ( sender, args ) => autoCloseTimer.Stop ( );
                        }

                        return messageBox.ShowDialog ( );
                }

                private static Window GetOwnerWindow ( )
                {
                        var activeWindow = Application.Current?.Windows
                                .OfType<Window>()
                                .FirstOrDefault ( window => window.IsActive );

                        if ( activeWindow != null )
                        {
                                return activeWindow;
                        }

                        return Application.Current?.MainWindow;
                }
        }
}
