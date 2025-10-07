using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using HtmlAgilityPack;

using UglyToad.PdfPig;

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

                        if (book != null)
                        {
                                // Build a sequence of content objects without assuming exact library types.
                                IEnumerable<object>? orderedContent = null;
                                if (book.ReadingOrder != null && (book.ReadingOrder as IEnumerable)?.GetEnumerator() != null)
                                {
                                        orderedContent = ((IEnumerable)book.ReadingOrder).Cast<object>();
                                }
                                else if (book.Content?.Html is IEnumerable htmlCollection)
                                {
                                        orderedContent = htmlCollection.Cast<object>();
                                }

                                if (orderedContent != null)
                                {
                                        foreach (var contentObj in orderedContent)
                                        {
                                                cancellationToken.ThrowIfCancellationRequested();

                                                if (contentObj == null)
                                                        continue;

                                                // contentObj may be either a text content file or a KeyValuePair-like entry.
                                                object? fileObj = contentObj;
                                                string? keyFromPair = null;

                                                var type = contentObj.GetType();
                                                var valueProp = type.GetProperty("Value");
                                                if (valueProp != null)
                                                {
                                                        // treat as KeyValuePair or similar
                                                        fileObj = valueProp.GetValue(contentObj);
                                                        var keyProp = type.GetProperty("Key");
                                                        if (keyProp != null)
                                                                keyFromPair = keyProp.GetValue(contentObj)?.ToString();
                                                }

                                                if (fileObj == null)
                                                        continue;

                                                string? htmlContent = null;
                                                try
                                                {
                                                        var contentProp = fileObj.GetType().GetProperty("Content");
                                                        if (contentProp != null)
                                                                htmlContent = contentProp.GetValue(fileObj) as string;
                                                }
                                                catch (TargetInvocationException)
                                                {
                                                        continue;
                                                }

                                                if (string.IsNullOrWhiteSpace(htmlContent))
                                                        continue;

                                                string text;
                                                try
                                                {
                                                        text = HtmlToPlainText(htmlContent);
                                                }
                                                catch (Exception ex)
                                                {
                                                        // Try to build a section identifier for error message
                                                        var sectionName = keyFromPair ?? fileObj.GetType().Name;
                                                        throw new InvalidOperationException($"Failed to convert HTML to text for EPUB section '{sectionName}'.", ex);
                                                }

                                                // Derive a section title using available metadata if present
                                                string? sectionTitle = null;
                                                var titleProp = fileObj.GetType().GetProperty("Title");
                                                if (titleProp != null)
                                                        sectionTitle = titleProp.GetValue(fileObj) as string;

                                                if (string.IsNullOrWhiteSpace(sectionTitle))
                                                {
                                                        var filenameProp = fileObj.GetType().GetProperty("FileName") ?? fileObj.GetType().GetProperty("FilePath");
                                                        if (filenameProp != null)
                                                        {
                                                                var filename = filenameProp.GetValue(fileObj) as string;
                                                                if (!string.IsNullOrWhiteSpace(filename))
                                                                        sectionTitle = Path.GetFileNameWithoutExtension(filename);
                                                        }
                                                }

                                                if (string.IsNullOrWhiteSpace(sectionTitle) && !string.IsNullOrWhiteSpace(keyFromPair))
                                                        sectionTitle = Path.GetFileNameWithoutExtension(keyFromPair);

                                                if (string.IsNullOrWhiteSpace(sectionTitle))
                                                        sectionTitle = "Section";

                                                var startIndex = AppendSectionContent(builder, sectionTitle, text);
                                                sections.Add(new DocumentSection(sectionTitle, startIndex, 0));
                                        }
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

                                        // Outline/bookmark extraction is omitted because the PdfPig API varies between versions.
                                        // Rely on per-page sections as a stable fallback.
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
