using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Dissonance.ViewModels;

namespace Dissonance.Windows.Views
{
        public partial class DocumentReaderView : UserControl
        {
                public DocumentReaderView ( )
                {
                        InitializeComponent ( );
                }

                private DocumentReaderViewModel? ViewModel => DataContext as DocumentReaderViewModel;

                private void DropZoneButton_DragEnter ( object sender, DragEventArgs e )
                {
                        HandleDrag ( e );
                }

                private void DropZoneButton_DragOver ( object sender, DragEventArgs e )
                {
                        HandleDrag ( e );
                }

                private void DropZoneButton_DragLeave ( object sender, DragEventArgs e )
                {
                        if ( ViewModel != null )
                        {
                                ViewModel.IsDropActive = false;
                        }
                }

                private async void DropZoneButton_Drop ( object sender, DragEventArgs e )
                {
                        if ( ViewModel == null )
                                return;

                        ViewModel.IsDropActive = false;

                        if ( e.Data != null )
                        {
                                await ViewModel.IngestDataObjectAsync ( e.Data );
                        }

                        e.Handled = true;
                }

                private void HandleDrag ( DragEventArgs e )
                {
                        if ( ViewModel == null )
                                return;

                        if ( ViewModel.CanAcceptDataObject ( e.Data ) )
                        {
                                e.Handled = true;
                                e.Effects = DragDropEffects.Copy;
                                ViewModel.IsDropActive = true;
                        }
                        else
                        {
                                e.Effects = DragDropEffects.None;
                                ViewModel.IsDropActive = false;
                        }
                }
        }
}
