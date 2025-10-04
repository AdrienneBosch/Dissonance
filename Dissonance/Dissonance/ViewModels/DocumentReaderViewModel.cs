using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

using Dissonance.Infrastructure.Commands;
using Dissonance.Services.DocumentReader;

using Microsoft.Win32;

namespace Dissonance.ViewModels
{
        public class DocumentReaderViewModel : INotifyPropertyChanged
        {
                private readonly IDocumentReaderService _documentReaderService;
                private readonly RelayCommandNoParam _clearDocumentCommand;
                private readonly RelayCommandNoParam _browseForDocumentCommand;
                private FlowDocument? _document;
                private string? _plainText;
                private string? _filePath;
                private string? _statusMessage;
                private bool _isBusy;
                private Exception? _lastError;

                public DocumentReaderViewModel(IDocumentReaderService documentReaderService)
                {
                        _documentReaderService = documentReaderService ?? throw new ArgumentNullException(nameof(documentReaderService));
                        _clearDocumentCommand = new RelayCommandNoParam(ClearDocument, () => !IsBusy && (IsDocumentLoaded || HasStatusMessage));
                        _browseForDocumentCommand = new RelayCommandNoParam(BrowseForDocument, () => !IsBusy);
                }

                public event PropertyChangedEventHandler? PropertyChanged;

                public FlowDocument? Document
                {
                        get => _document;
                        private set
                        {
                                if (ReferenceEquals(_document, value))
                                        return;

                                _document = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(IsDocumentLoaded));
                                OnPropertyChanged(nameof(CanReadDocument));
                                OnPropertyChanged(nameof(FileName));
                                UpdateCommandStates();
                        }
                }

                public string? PlainText
                {
                        get => _plainText;
                        private set
                        {
                                if (_plainText == value)
                                        return;

                                _plainText = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(HasPlainText));
                                OnPropertyChanged(nameof(WordCount));
                                OnPropertyChanged(nameof(CharacterCount));
                                OnPropertyChanged(nameof(CanReadDocument));
                        }
                }

                public string? FilePath
                {
                        get => _filePath;
                        set
                        {
                                if (_filePath == value)
                                        return;

                                _filePath = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(FileName));
                        }
                }

                public string? FileName => string.IsNullOrWhiteSpace(FilePath) ? null : Path.GetFileName(FilePath);

                public bool IsDocumentLoaded => Document != null;

                public bool HasPlainText => !string.IsNullOrWhiteSpace(PlainText);

                public bool CanReadDocument => IsDocumentLoaded && HasPlainText;

                public int WordCount => _plainText?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

                public int CharacterCount => _plainText?.Length ?? 0;

                public bool IsBusy
                {
                        get => _isBusy;
                        private set
                        {
                                if (_isBusy == value)
                                        return;

                                _isBusy = value;
                                OnPropertyChanged();
                                UpdateCommandStates();
                        }
                }

                public string? StatusMessage
                {
                        get => _statusMessage;
                        private set
                        {
                                if (_statusMessage == value)
                                        return;

                                _statusMessage = value;
                                OnPropertyChanged();
                                OnPropertyChanged(nameof(HasStatusMessage));
                                UpdateCommandStates();
                        }
                }

                public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

                public Exception? LastError
                {
                        get => _lastError;
                        private set
                        {
                                if (ReferenceEquals(_lastError, value))
                                        return;

                                _lastError = value;
                                OnPropertyChanged();
                        }
                }

                public ICommand ClearDocumentCommand => _clearDocumentCommand;

                public ICommand BrowseForDocumentCommand => _browseForDocumentCommand;

                public async Task<bool> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                {
                        if (string.IsNullOrWhiteSpace(filePath))
                                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

                        try
                        {
                                IsBusy = true;
                                LastError = null;
                                StatusMessage = null;

                                var result = await _documentReaderService.ReadDocumentAsync(filePath, cancellationToken);

                                ApplyResult(result);
                                StatusMessage = null;
                                return true;
                        }
                        catch (OperationCanceledException)
                        {
                                ClearDocument();
                                StatusMessage = "Document loading was canceled.";
                                throw;
                        }
                        catch (Exception ex)
                        {
                                ClearDocument();
                                LastError = ex;
                                StatusMessage = ex.Message;
                                return false;
                        }
                        finally
                        {
                                IsBusy = false;
                        }
                }

                public void ClearDocument()
                {
                        Document = null;
                        PlainText = null;
                        FilePath = null;
                        LastError = null;
                        StatusMessage = null;
                }

                private async void BrowseForDocument()
                {
                        var dialog = new OpenFileDialog
                        {
                                Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*",
                                CheckFileExists = true,
                                CheckPathExists = true,
                                Title = "Open document",
                                Multiselect = false
                        };

                        if (!string.IsNullOrWhiteSpace(FilePath))
                        {
                                var directory = Path.GetDirectoryName(FilePath);
                                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                                        dialog.InitialDirectory = directory;
                        }

                        var result = dialog.ShowDialog();
                        if (result != true)
                                return;

                        try
                        {
                                await LoadDocumentAsync(dialog.FileName);
                        }
                        catch (OperationCanceledException)
                        {
                                // Cancellation has already updated the status message; swallow to avoid surfacing to the UI.
                        }
                }

                private void ApplyResult(DocumentReadResult result)
                {
                        if (result == null)
                                throw new ArgumentNullException(nameof(result));

                        Document = CreateFlowDocument(result.PlainText);
                        PlainText = result.PlainText;
                        FilePath = result.FilePath;
                        LastError = null;
                }

                private void UpdateCommandStates()
                {
                        _clearDocumentCommand.RaiseCanExecuteChanged();
                        _browseForDocumentCommand.RaiseCanExecuteChanged();
                }

                private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }

                private static FlowDocument CreateFlowDocument(string content)
                {
                        var document = new FlowDocument();
                        if (string.IsNullOrEmpty(content))
                        {
                                document.Blocks.Add(new Paragraph(new Run(string.Empty)));
                                return document;
                        }

                        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
                        var lines = normalized.Split('\n');
                        foreach (var line in lines)
                        {
                                document.Blocks.Add(new Paragraph(new Run(line)));
                        }

                        return document;
                }
        }
}
