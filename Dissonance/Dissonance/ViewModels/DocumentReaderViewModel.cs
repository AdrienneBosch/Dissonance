using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Dissonance.Infrastructure.Commands;
using Dissonance.Infrastructure.Constants;
using Dissonance.Services.DocumentService;
using Dissonance.Services.MessageService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;

using Microsoft.Win32;

namespace Dissonance.ViewModels
{
        public class DocumentReaderViewModel : INotifyPropertyChanged
        {
                private const int SegmentCharacterLimit = 1200;
                private readonly IDocumentTextExtractor _documentTextExtractor;
                private readonly IMessageService _messageService;
                private readonly ISettingsService _settingsService;
                private readonly ITTSService _ttsService;
                private readonly Dispatcher _dispatcher;
                private readonly RelayCommand _browseForFileCommand;
                private readonly RelayCommand _readDocumentCommand;
                private readonly RelayCommand _stopReadingCommand;
                private readonly RelayCommand _clearDocumentCommand;
                private CancellationTokenSource? _loadCancellation;
                private string? _documentText;
                private string? _selectedFileName;
                private string? _documentPreview;
                private string? _documentDetails;
                private string _statusMessage = "Drop a document or browse to get started.";
                private bool _isBusy;
                private bool _isDropActive;
                private bool _isReading;
                private IReadOnlyList<string> _segments = Array.Empty<string> ( );
                private int _currentSegmentIndex = -1;
                private int _completedSegments;

                public DocumentReaderViewModel (
                        IDocumentTextExtractor documentTextExtractor,
                        IMessageService messageService,
                        ISettingsService settingsService,
                        ITTSService ttsService )
                {
                        _documentTextExtractor = documentTextExtractor ?? throw new ArgumentNullException ( nameof ( documentTextExtractor ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
                        _settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
                        _ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );

                        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                        _ttsService.SpeechCompleted += OnSpeechCompleted;

                        _browseForFileCommand = new RelayCommand ( _ => BrowseForFile ( ), _ => !IsBusy );
                        _readDocumentCommand = new RelayCommand ( _ => StartReading ( ), _ => CanStartReading ( ) );
                        _stopReadingCommand = new RelayCommand ( _ => StopReading ( ), _ => IsReading );
                        _clearDocumentCommand = new RelayCommand ( _ => ClearDocument ( ), _ => CanClearDocument ( ) );
                }

                public event PropertyChangedEventHandler? PropertyChanged;

                public ICommand BrowseForFileCommand => _browseForFileCommand;

                public ICommand ReadDocumentCommand => _readDocumentCommand;

                public ICommand StopReadingCommand => _stopReadingCommand;

                public ICommand ClearDocumentCommand => _clearDocumentCommand;

                public string SupportedFormatsDescription => string.Join ( ", ", _documentTextExtractor.SupportedFileExtensions.Select ( ext => ext.TrimStart ( '.' ).ToUpperInvariant ( ) ) );

                public string? SelectedFileName
                {
                        get => _selectedFileName;
                        private set
                        {
                                if ( _selectedFileName == value )
                                        return;

                                _selectedFileName = value;
                                OnPropertyChanged ( nameof ( SelectedFileName ) );
                        }
                }

                public string? DocumentPreview
                {
                        get => _documentPreview;
                        private set
                        {
                                if ( _documentPreview == value )
                                        return;

                                _documentPreview = value;
                                OnPropertyChanged ( nameof ( DocumentPreview ) );
                        }
                }

                public string? DocumentDetails
                {
                        get => _documentDetails;
                        private set
                        {
                                if ( _documentDetails == value )
                                        return;

                                _documentDetails = value;
                                OnPropertyChanged ( nameof ( DocumentDetails ) );
                        }
                }

                public string StatusMessage
                {
                        get => _statusMessage;
                        private set
                        {
                                if ( _statusMessage == value )
                                        return;

                                _statusMessage = value;
                                OnPropertyChanged ( nameof ( StatusMessage ) );
                        }
                }

                public bool IsBusy
                {
                        get => _isBusy;
                        private set
                        {
                                if ( _isBusy == value )
                                        return;

                                _isBusy = value;
                                OnPropertyChanged ( nameof ( IsBusy ) );
                                OnPropertyChanged ( nameof ( ProgressLabel ) );
                                OnPropertyChanged ( nameof ( IsProgressVisible ) );
                                UpdateCommandStates ( );
                        }
                }

                public bool IsDropActive
                {
                        get => _isDropActive;
                        set
                        {
                                if ( _isDropActive == value )
                                        return;

                                _isDropActive = value;
                                OnPropertyChanged ( nameof ( IsDropActive ) );
                        }
                }

                public bool HasDocument => !string.IsNullOrEmpty ( _documentText );

                public bool IsReading
                {
                        get => _isReading;
                        private set
                        {
                                if ( _isReading == value )
                                        return;

                                _isReading = value;
                                OnPropertyChanged ( nameof ( IsReading ) );
                                OnPropertyChanged ( nameof ( ProgressLabel ) );
                                OnPropertyChanged ( nameof ( IsProgressVisible ) );
                                UpdateCommandStates ( );
                        }
                }

                public int TotalSegments => _segments.Count;

                public double ReadingProgress => TotalSegments == 0 ? 0 : ( double ) _completedSegments / TotalSegments;

                public string ProgressLabel
                {
                        get
                        {
                                if ( IsBusy )
                                        return "Loading document...";

                                if ( TotalSegments == 0 )
                                        return "No document loaded";

                                if ( !IsReading && _completedSegments == 0 )
                                        return "Ready to read";

                                if ( _completedSegments >= TotalSegments )
                                        return "Playback finished";

                                var currentSegmentNumber = Math.Max ( 1, _currentSegmentIndex + 1 );
                                return $"Segment {currentSegmentNumber} of {TotalSegments}";
                        }
                }

                public bool IsProgressVisible => IsBusy || IsReading || _completedSegments > 0;

                public void OnWindowClosing ( )
                {
                        CancelOngoingLoad ( );
                        StopReading ( );
                }

                public async Task IngestDataObjectAsync ( IDataObject? dataObject )
                {
                        if ( dataObject == null )
                                return;

                        if ( !dataObject.GetDataPresent ( DataFormats.FileDrop ) )
                        {
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.DocumentServiceWarning, "Please drop document files only." );
                                return;
                        }

                        if ( dataObject.GetData ( DataFormats.FileDrop ) is not string[] files || files.Length == 0 )
                        {
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.DocumentServiceWarning, "No files were provided." );
                                return;
                        }

                        var file = files.FirstOrDefault ( IsSupportedFile );
                        if ( string.IsNullOrEmpty ( file ) )
                        {
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.DocumentServiceWarning, "No supported documents were found in the drop." );
                                return;
                        }

                        await LoadDocumentAsync ( file );
                }

                public bool CanAcceptDataObject ( IDataObject? dataObject )
                {
                        if ( dataObject == null || !dataObject.GetDataPresent ( DataFormats.FileDrop ) )
                                return false;

                        if ( dataObject.GetData ( DataFormats.FileDrop ) is not string[] files || files.Length == 0 )
                                return false;

                        return files.Any ( IsSupportedFile );
                }

                public async Task LoadDocumentAsync ( string filePath )
                {
                        if ( string.IsNullOrWhiteSpace ( filePath ) )
                                return;

                        if ( !File.Exists ( filePath ) )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.DocumentServiceError, $"The file '{filePath}' could not be found." );
                                return;
                        }

                        if ( !IsSupportedFile ( filePath ) )
                        {
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.DocumentServiceWarning, "Only .TXT, .RTF, or .DOCX files are supported." );
                                return;
                        }

                        CancelOngoingLoad ( );
                        var cancellation = new CancellationTokenSource ( );
                        _loadCancellation = cancellation;

                        try
                        {
                                IsBusy = true;
                                StatusMessage = $"Loading {Path.GetFileName ( filePath )}...";

                                var sanitizedText = await _documentTextExtractor.ExtractTextAsync ( filePath, cancellation.Token );

                                if ( cancellation.IsCancellationRequested )
                                        return;

                                if ( string.IsNullOrEmpty ( sanitizedText ) )
                                {
                                        _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.DocumentServiceWarning, "The selected file did not contain readable text." );
                                        ResetDocumentState ( );
                                        StatusMessage = "The selected file did not contain readable text.";
                                        return;
                                }

                                ApplyLoadedDocument ( filePath, sanitizedText );
                        }
                        catch ( OperationCanceledException )
                        {
                                // Loading was cancelled intentionally.
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.DocumentServiceError, $"Failed to read '{Path.GetFileName ( filePath )}'.", ex );
                                ResetDocumentState ( );
                                StatusMessage = "Unable to load the selected document.";
                        }
                        finally
                        {
                                if ( ReferenceEquals ( _loadCancellation, cancellation ) )
                                {
                                        _loadCancellation.Dispose ( );
                                        _loadCancellation = null;
                                }

                                IsBusy = false;
                        }
                }

                public void ClearDocument ( )
                {
                        if ( !HasDocument )
                                return;

                        StopReadingInternal ( true );
                        ResetDocumentState ( );
                        StatusMessage = "Document cleared.";
                }

                public void StopReading ( )
                {
                        if ( !IsReading )
                                return;

                        StopReadingInternal ( true );
                        StatusMessage = "Playback stopped.";
                }

                private void ApplyLoadedDocument ( string filePath, string sanitizedText )
                {
                        StopReadingInternal ( false );

                        _documentText = sanitizedText;
                        SelectedFileName = Path.GetFileName ( filePath );

                        var previewLength = Math.Min ( 800, sanitizedText.Length );
                        DocumentPreview = previewLength == sanitizedText.Length
                                ? sanitizedText
                                : sanitizedText[..previewLength] + "…";

                        var wordCount = sanitizedText.Split ( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ).Length;
                        DocumentDetails = string.Format ( CultureInfo.CurrentCulture, "{0:N0} words • {1:N0} characters", wordCount, sanitizedText.Length );

                        _segments = SegmentDocument ( sanitizedText );
                        _currentSegmentIndex = -1;
                        _completedSegments = 0;
                        OnPropertyChanged ( nameof ( TotalSegments ) );
                        OnPropertyChanged ( nameof ( HasDocument ) );
                        OnPropertyChanged ( nameof ( ReadingProgress ) );
                        OnPropertyChanged ( nameof ( ProgressLabel ) );
                        OnPropertyChanged ( nameof ( IsProgressVisible ) );

                        StatusMessage = TotalSegments > 0
                                ? "Document loaded. Ready to read."
                                : "Document loaded, but nothing to read.";

                        UpdateCommandStates ( );
                }

                private void StartReading ( )
                {
                        if ( !CanStartReading ( ) )
                                return;

                        if ( TotalSegments == 0 )
                        {
                                _messageService.DissonanceMessageBoxShowWarning ( MessageBoxTitles.DocumentServiceWarning, "The document does not contain readable content." );
                                return;
                        }

                        var settings = _settingsService.GetCurrentSettings ( );
                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );

                        _completedSegments = 0;
                        OnPropertyChanged ( nameof ( ReadingProgress ) );
                        OnPropertyChanged ( nameof ( ProgressLabel ) );
                        OnPropertyChanged ( nameof ( IsProgressVisible ) );
                        SpeakSegment ( 0 );
                }

                private void SpeakSegment ( int index )
                {
                        if ( index < 0 || index >= TotalSegments )
                                return;

                        _currentSegmentIndex = index;
                        OnPropertyChanged ( nameof ( ProgressLabel ) );

                        var segmentText = _segments[index];
                        StatusMessage = $"Reading segment {index + 1} of {TotalSegments}...";
                        IsReading = true;

                        var prompt = _ttsService.Speak ( segmentText );
                        if ( prompt == null )
                        {
                                IsReading = false;
                                StatusMessage = "Unable to start playback.";
                                return;
                        }

                        UpdateCommandStates ( );
                }

                private void StopReadingInternal ( bool stopSynthesizer, bool resetProgress = true )
                {
                        if ( stopSynthesizer )
                        {
                                try
                                {
                                        _ttsService.Stop ( );
                                }
                                catch
                                {
                                        // The service already reports errors; swallow here to avoid duplicate messaging.
                                }
                        }

                        _currentSegmentIndex = -1;
                        if ( resetProgress )
                                _completedSegments = 0;
                        IsReading = false;
                        OnPropertyChanged ( nameof ( ReadingProgress ) );
                        OnPropertyChanged ( nameof ( ProgressLabel ) );
                        OnPropertyChanged ( nameof ( IsProgressVisible ) );
                }

                private void ResetDocumentState ( )
                {
                        _documentText = null;
                        SelectedFileName = null;
                        DocumentPreview = null;
                        DocumentDetails = null;
                        _segments = Array.Empty<string> ( );
                        _currentSegmentIndex = -1;
                        _completedSegments = 0;
                        OnPropertyChanged ( nameof ( HasDocument ) );
                        OnPropertyChanged ( nameof ( TotalSegments ) );
                        OnPropertyChanged ( nameof ( ReadingProgress ) );
                        OnPropertyChanged ( nameof ( ProgressLabel ) );
                        OnPropertyChanged ( nameof ( IsProgressVisible ) );
                        StatusMessage = "Drop a document or browse to get started.";
                        UpdateCommandStates ( );
                }

                private bool CanStartReading ( ) => HasDocument && !IsBusy && !IsReading;

                private bool CanClearDocument ( ) => HasDocument && !IsBusy;

                private void CancelOngoingLoad ( )
                {
                        if ( _loadCancellation == null )
                                return;

                        if ( !_loadCancellation.IsCancellationRequested )
                        {
                                _loadCancellation.Cancel ( );
                        }

                        _loadCancellation.Dispose ( );
                        _loadCancellation = null;
                }

                private bool IsSupportedFile ( string filePath )
                {
                        var extension = Path.GetExtension ( filePath )?.ToLowerInvariant ( );
                        return extension != null && _documentTextExtractor.SupportedFileExtensions.Contains ( extension );
                }

                private static IReadOnlyList<string> SegmentDocument ( string text )
                {
                        if ( string.IsNullOrWhiteSpace ( text ) )
                                return Array.Empty<string> ( );

                        var segments = new List<string> ( );
                        var words = text.Split ( ' ' );
                        var builder = new List<string> ( );
                        var currentLength = 0;

                        foreach ( var word in words )
                        {
                                if ( string.IsNullOrWhiteSpace ( word ) )
                                        continue;

                                var candidateLength = currentLength + ( currentLength == 0 ? 0 : 1 ) + word.Length;
                                if ( candidateLength > SegmentCharacterLimit && builder.Count > 0 )
                                {
                                        segments.Add ( string.Join ( " ", builder ) );
                                        builder.Clear ( );
                                        currentLength = 0;
                                }

                                builder.Add ( word );
                                currentLength = currentLength == 0 ? word.Length : currentLength + 1 + word.Length;
                        }

                        if ( builder.Count > 0 )
                        {
                                segments.Add ( string.Join ( " ", builder ) );
                        }

                        return segments;
                }

                private void UpdateCommandStates ( )
                {
                        _browseForFileCommand.RaiseCanExecuteChanged ( );
                        _readDocumentCommand.RaiseCanExecuteChanged ( );
                        _stopReadingCommand.RaiseCanExecuteChanged ( );
                        _clearDocumentCommand.RaiseCanExecuteChanged ( );
                }

                private void OnSpeechCompleted ( object? sender, System.Speech.Synthesis.SpeakCompletedEventArgs e )
                {
                        if ( !_dispatcher.CheckAccess ( ) )
                        {
                                _dispatcher.BeginInvoke ( new Action<object?, System.Speech.Synthesis.SpeakCompletedEventArgs> ( OnSpeechCompleted ), sender, e );
                                return;
                        }

                        if ( !IsReading )
                                return;

                        if ( e.Cancelled )
                        {
                                StopReadingInternal ( false );
                                StatusMessage = "Playback cancelled.";
                                return;
                        }

                        _completedSegments++;
                        OnPropertyChanged ( nameof ( ReadingProgress ) );

                        if ( _completedSegments >= TotalSegments )
                        {
                                StopReadingInternal ( false, resetProgress: false );
                                StatusMessage = "Finished reading the document.";
                                return;
                        }

                        var nextIndex = _completedSegments;
                        SpeakSegment ( nextIndex );
                        if ( !IsReading )
                                return;
                        OnPropertyChanged ( nameof ( ReadingProgress ) );
                        OnPropertyChanged ( nameof ( ProgressLabel ) );
                        OnPropertyChanged ( nameof ( IsProgressVisible ) );
                }

                private void BrowseForFile ( )
                {
                        var dialog = new OpenFileDialog
                        {
                                Filter = "Documents (*.txt;*.rtf;*.docx)|*.txt;*.rtf;*.docx",
                                CheckFileExists = true,
                                Title = "Select a document to read"
                        };

                        if ( dialog.ShowDialog ( ) == true )
                        {
                                _ = LoadDocumentAsync ( dialog.FileName );
                        }
                }

                protected virtual void OnPropertyChanged ( string propertyName )
                {
                        PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propertyName ) );
                }
        }
}
