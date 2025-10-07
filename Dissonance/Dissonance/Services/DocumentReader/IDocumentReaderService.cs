using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace Dissonance.Services.DocumentReader
{
        public interface IDocumentReaderService
        {
                Task<DocumentReadResult> ReadDocumentAsync(string filePath, CancellationToken cancellationToken = default);
        }

        public sealed class DocumentReadResult
        {
                public DocumentReadResult(string filePath, string plainText)
                        : this(filePath, null, plainText, null)
                {
                }

                public DocumentReadResult(string filePath, FlowDocument? document, string plainText)
                        : this(filePath, document, plainText, null)
                {
                }

                public DocumentReadResult(string filePath, FlowDocument? document, string plainText, IReadOnlyList<DocumentSection>? sections)
                {
                        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
                        Document = document;
                        PlainText = plainText ?? string.Empty;
                        Sections = sections ?? Array.Empty<DocumentSection>();
                }

                public string FilePath { get; }

                public string FileName => Path.GetFileName(FilePath);

                public FlowDocument? Document { get; }

                public string PlainText { get; }

                public IReadOnlyList<DocumentSection> Sections { get; }

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

        public sealed class DocumentSection
        {
                public DocumentSection(string title, int startCharacterIndex, int level = 0)
                {
                        Title = string.IsNullOrWhiteSpace(title) ? "Untitled section" : title.Trim();
                        StartCharacterIndex = Math.Max(0, startCharacterIndex);
                        Level = Math.Max(0, level);
                }

                public string Title { get; }

                public int StartCharacterIndex { get; }

                public int Level { get; }

                public Thickness Indent => new(Level * 16, 0, 0, 0);
        }
}
