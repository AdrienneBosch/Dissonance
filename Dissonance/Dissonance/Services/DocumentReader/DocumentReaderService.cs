using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using HtmlAgilityPack;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Outline;

using VersOne.Epub;

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
                        if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
                                return await ReadPlainTextAsync(filePath, cancellationToken).ConfigureAwait(false);

                        if (string.Equals(extension, ".epub", StringComparison.OrdinalIgnoreCase))
                                return await ReadEpubAsync(filePath, cancellationToken).ConfigureAwait(false);

                        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                                return await ReadPdfAsync(filePath, cancellationToken).ConfigureAwait(false);

                        throw new NotSupportedException($"The document type '{extension}' is not supported.");
                }

                private static async Task<DocumentReadResult> ReadPlainTextAsync(string filePath, CancellationToken cancellationToken)
                {
                        cancellationToken.ThrowIfCancellationRequested();

                        var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

                        cancellationToken.ThrowIfCancellationRequested();

                        var normalized = NormalizeLineEndings(content);
                        return new DocumentReadResult(filePath, null, normalized);
                }

                private static async Task<DocumentReadResult> ReadEpubAsync(string filePath, CancellationToken cancellationToken)
                {
                        cancellationToken.ThrowIfCancellationRequested();

                        var book = await EpubReader.ReadBookAsync(filePath).ConfigureAwait(false);

                        cancellationToken.ThrowIfCancellationRequested();

                        var sections = new List<DocumentSection>();
                        var builder = new StringBuilder();

                        if (book?.Chapters != null)
                        {
                                foreach (var chapter in book.Chapters)
                                {
                                        AppendEpubChapter(chapter, sections, builder, 0, cancellationToken);
                                }
                        }

                        if (builder.Length == 0 && book?.Content?.Html != null)
                        {
                                foreach (var htmlContent in book.Content.Html.Values)
                                {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        var text = HtmlToPlainText(htmlContent.Content);
                                        if (string.IsNullOrWhiteSpace(text))
                                                continue;

                                        var title = !string.IsNullOrWhiteSpace(htmlContent.FileName) ? Path.GetFileNameWithoutExtension(htmlContent.FileName) : "Section";
                                        var startIndex = AppendSectionContent(builder, title, text);
                                        sections.Add(new DocumentSection(title, startIndex, 0));
                                }
                        }

                        var plainText = NormalizeLineEndings(builder.ToString());
                        return new DocumentReadResult(filePath, null, plainText, sections);
                }

                private static async Task<DocumentReadResult> ReadPdfAsync(string filePath, CancellationToken cancellationToken)
                {
                        return await Task.Run(() =>
                        {
                                cancellationToken.ThrowIfCancellationRequested();

                                var sections = new List<DocumentSection>();
                                var builder = new StringBuilder();
                                var pageOffsets = new Dictionary<int, int>();

                                using (var document = PdfDocument.Open(filePath))
                                {
                                        var totalPages = document.NumberOfPages;
                                        for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
                                        {
                                                cancellationToken.ThrowIfCancellationRequested();

                                                pageOffsets[pageNumber] = builder.Length;
                                                var page = document.GetPage(pageNumber);
                                                var text = NormalizeWhitespace(page.Text);
                                                if (!string.IsNullOrWhiteSpace(text))
                                                {
                                                        if (builder.Length > 0)
                                                                builder.AppendLine();

                                                        builder.AppendLine(text);
                                                }
                                        }

                                        var outlines = document.GetOutlines();
                                        if (outlines != null && outlines.Count > 0)
                                                FlattenPdfOutlines(outlines, 0, pageOffsets, sections);
                                }

                                if (sections.Count == 0)
                                {
                                        foreach (var pageNumber in pageOffsets.Keys.OrderBy(n => n))
                                        {
                                                sections.Add(new DocumentSection($"Page {pageNumber}", pageOffsets[pageNumber], 0));
                                        }
                                }

                                var plainText = NormalizeLineEndings(builder.ToString());
                                return new DocumentReadResult(filePath, null, plainText, sections);
                        }, cancellationToken).ConfigureAwait(false);
                }

                private static void AppendEpubChapter(EpubChapter? chapter, ICollection<DocumentSection> sections, StringBuilder builder, int level, CancellationToken cancellationToken)
                {
                        if (chapter == null)
                                return;

                        cancellationToken.ThrowIfCancellationRequested();

                        var title = !string.IsNullOrWhiteSpace(chapter.Title) ? chapter.Title!.Trim() : null;
                        var body = HtmlToPlainText(chapter.HtmlContent);
                        var hasContent = !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body);

                        if (hasContent)
                        {
                                var startIndex = AppendSectionContent(builder, title, body);
                                sections.Add(new DocumentSection(title ?? "Chapter", startIndex, level));
                        }

                        if (chapter.SubChapters != null)
                        {
                                foreach (var subChapter in chapter.SubChapters)
                                {
                                        AppendEpubChapter(subChapter, sections, builder, level + 1, cancellationToken);
                                }
                        }
                }

                private static string HtmlToPlainText(string? html)
                {
                        if (string.IsNullOrWhiteSpace(html))
                                return string.Empty;

                        var document = new HtmlDocument();
                        document.LoadHtml(html);
                        var text = document.DocumentNode.InnerText ?? string.Empty;
                        text = WebUtility.HtmlDecode(text);
                        return NormalizeWhitespace(text);
                }

                private static int AppendSectionContent(StringBuilder builder, string? title, string? body)
                {
                        var hasTitle = !string.IsNullOrWhiteSpace(title);
                        var hasBody = !string.IsNullOrWhiteSpace(body);

                        if (!hasTitle && !hasBody)
                                return builder.Length;

                        if (builder.Length > 0)
                                builder.AppendLine();

                        var startIndex = builder.Length;

                        if (hasTitle)
                                builder.AppendLine(title!.Trim());

                        if (hasBody)
                        {
                                if (builder.Length > startIndex && builder[builder.Length - 1] != '\n')
                                        builder.AppendLine();

                                builder.AppendLine(body!.Trim());
                        }

                        return startIndex;
                }

                private static void FlattenPdfOutlines(IReadOnlyList<BookmarkNode> outlines, int level, IReadOnlyDictionary<int, int> pageOffsets, ICollection<DocumentSection> sections)
                {
                        if (outlines == null)
                                return;

                        foreach (var outline in outlines)
                        {
                                if (!string.IsNullOrWhiteSpace(outline.Title) && outline.PageNumber.HasValue && pageOffsets.TryGetValue(outline.PageNumber.Value, out var index))
                                {
                                        sections.Add(new DocumentSection(outline.Title, index, level));
                                }

                                if (outline.Children != null && outline.Children.Count > 0)
                                {
                                        FlattenPdfOutlines(outline.Children, level + 1, pageOffsets, sections);
                                }
                        }
                }

                private static string NormalizeWhitespace(string? text)
                {
                        if (string.IsNullOrWhiteSpace(text))
                                return string.Empty;

                        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
                        normalized = Regex.Replace(normalized, "[\t\f\v\u00A0]+", " ");
                        normalized = Regex.Replace(normalized, " {2,}", " ");
                        normalized = Regex.Replace(normalized, "\n{3,}", "\n\n");
                        return normalized.Trim();
                }

                private static string NormalizeLineEndings(string? text)
                {
                        if (string.IsNullOrEmpty(text))
                                return string.Empty;

                        return text.Replace("\r\n", "\n").Replace('\r', '\n');
                }
        }
}
