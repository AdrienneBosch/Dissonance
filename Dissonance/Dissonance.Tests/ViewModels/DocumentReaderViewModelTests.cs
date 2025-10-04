using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

using Dissonance.Services.DocumentReader;
using Dissonance.ViewModels;

using Xunit;

namespace Dissonance.Tests.ViewModels
{
        public class DocumentReaderViewModelTests
        {
                [Fact]
                public async Task LoadDocumentAsync_PopulatesPropertiesOnSuccess()
                {
                        var result = new DocumentReadResult("sample.txt", new FlowDocument(new Paragraph(new Run("Hello world"))), "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var viewModel = new DocumentReaderViewModel(service);

                        var success = await viewModel.LoadDocumentAsync(result.FilePath);

                        Assert.True(success);
                        Assert.NotNull(viewModel.Document);
                        Assert.Equal(result.PlainText, viewModel.PlainText);
                        Assert.Equal(result.FilePath, viewModel.FilePath);
                        Assert.Equal("sample.txt", viewModel.FileName);
                        Assert.True(viewModel.IsDocumentLoaded);
                        Assert.True(viewModel.CanReadDocument);
                        Assert.Equal(2, viewModel.WordCount);
                        Assert.Equal(result.PlainText.Length, viewModel.CharacterCount);
                        Assert.Null(viewModel.StatusMessage);
                        Assert.Null(viewModel.LastError);
                        Assert.True(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public async Task LoadDocumentAsync_WhenServiceThrows_ReturnsFalseAndSetsError()
                {
                        var service = new FailingDocumentReaderService(new InvalidOperationException("boom"));
                        var viewModel = new DocumentReaderViewModel(service);

                        var success = await viewModel.LoadDocumentAsync("missing.txt");

                        Assert.False(success);
                        Assert.Null(viewModel.Document);
                        Assert.Null(viewModel.PlainText);
                        Assert.False(viewModel.IsDocumentLoaded);
                        Assert.False(viewModel.CanReadDocument);
                        Assert.Equal("boom", viewModel.StatusMessage);
                        Assert.IsType<InvalidOperationException>(viewModel.LastError);
                        Assert.False(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public void ClearDocumentCommand_ResetsState()
                {
                        var result = new DocumentReadResult("sample.txt", new FlowDocument(new Paragraph(new Run("Hello world"))), "Hello world");
                        var service = new StubDocumentReaderService(result);
                        var viewModel = new DocumentReaderViewModel(service);

                        viewModel.ClearDocumentCommand.Execute(null);

                        Assert.Null(viewModel.Document);
                        Assert.Null(viewModel.PlainText);
                        Assert.Null(viewModel.FilePath);
                        Assert.False(viewModel.IsDocumentLoaded);
                        Assert.False(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                [Fact]
                public async Task CommandsReflectBusyStateWhileLoading()
                {
                        var tcs = new TaskCompletionSource<DocumentReadResult>();
                        var service = new PendingDocumentReaderService(tcs);
                        var viewModel = new DocumentReaderViewModel(service);

                        var loadTask = viewModel.LoadDocumentAsync("sample.txt");

                        Assert.False(viewModel.ClearDocumentCommand.CanExecute(null));
                        Assert.False(viewModel.BrowseForDocumentCommand.CanExecute(null));

                        tcs.SetResult(new DocumentReadResult("sample.txt", new FlowDocument(), string.Empty));
                        await loadTask;

                        Assert.True(viewModel.BrowseForDocumentCommand.CanExecute(null));
                }

                private sealed class StubDocumentReaderService : IDocumentReaderService
                {
                        private readonly DocumentReadResult _result;

                        public StubDocumentReaderService(DocumentReadResult result)
                        {
                                _result = result;
                        }

                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return Task.FromResult(_result);
                        }
                }

                private sealed class FailingDocumentReaderService : IDocumentReaderService
                {
                        private readonly Exception _exception;

                        public FailingDocumentReaderService(Exception exception)
                        {
                                _exception = exception;
                        }

                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return Task.FromException<DocumentReadResult>(_exception);
                        }
                }

                private sealed class PendingDocumentReaderService : IDocumentReaderService
                {
                        private readonly TaskCompletionSource<DocumentReadResult> _completion;

                        public PendingDocumentReaderService(TaskCompletionSource<DocumentReadResult> completion)
                        {
                                _completion = completion;
                        }

                        public Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                        {
                                return _completion.Task;
                        }
                }
        }
}
