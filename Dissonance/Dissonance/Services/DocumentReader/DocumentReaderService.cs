using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dissonance.Services.DocumentReader
{
        public class DocumentReaderService : IDocumentReaderService
        {
                public async Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
                {
                        if (string.IsNullOrWhiteSpace(filePath))
                                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

                        if (!File.Exists(filePath))
                                throw new FileNotFoundException("The specified document could not be found.", filePath);

                        var extension = Path.GetExtension(filePath);
                        if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
                                throw new NotSupportedException($"The document type '{extension}' is not supported.");

                        cancellationToken.ThrowIfCancellationRequested();

                        string content;
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                        {
                                content = await reader.ReadToEndAsync().ConfigureAwait(false);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        return new DocumentReadResult(filePath, content ?? string.Empty);
                }
        }
}
