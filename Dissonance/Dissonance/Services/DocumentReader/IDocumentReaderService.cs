using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Dissonance.Services.DocumentReader
{
        public interface IDocumentReaderService
        {
                Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default);
        }

        public sealed class DocumentReadResult
        {
                public DocumentReadResult(string filePath, FlowDocument document, string plainText)
                {
                        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
                        Document = document ?? throw new ArgumentNullException(nameof(document));
                        PlainText = plainText ?? string.Empty;
                }

                public string FilePath { get; }

                public string FileName => Path.GetFileName(FilePath);

                public FlowDocument Document { get; }

                public string PlainText { get; }

                public int CharacterCount => PlainText.Length;

                public int WordCount
                {
                        get
                        {
                                if (string.IsNullOrWhiteSpace(PlainText))
                                        return 0;

                                var separators = new[] { ' ', '\t', '\r', '\n' };
                                return PlainText
                                        .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                                        .Length;
                        }
                }
        }
}
