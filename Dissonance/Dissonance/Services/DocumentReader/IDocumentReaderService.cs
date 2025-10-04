using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dissonance.Services.DocumentReader
{
        public interface IDocumentReaderService
        {
                Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default);
        }

        public sealed class DocumentReadResult
        {
                public DocumentReadResult(string filePath, string plainText)
                {
                        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
                        PlainText = plainText ?? string.Empty;
                }

                public string FilePath { get; }

                public string FileName => Path.GetFileName(FilePath);

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
