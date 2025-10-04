using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

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

                        var document = CreateFlowDocument(content ?? string.Empty);
                        return new DocumentReadResult(filePath, document, content ?? string.Empty);
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
