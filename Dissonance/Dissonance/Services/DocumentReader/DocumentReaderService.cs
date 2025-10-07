using System;
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
                                try
                                {
                                        var contentProp = book.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance);
                                        if (contentProp != null)
                                        {
                                                var contentObj = contentProp.GetValue(book);
                                                if (contentObj != null)
                                                {
                                                        var htmlProp = contentObj.GetType().GetProperty("Html", BindingFlags.Public | BindingFlags.Instance);
                                                        if (htmlProp != null)
                                                        {
                                                                var htmlCollection = htmlProp.GetValue(contentObj);
                                                                if (htmlCollection != null)
                                                                {
                                                                        IEnumerable<object>? items = null;

                                                                        // Try direct 'Values' property
                                                                        var valuesProp = htmlCollection.GetType().GetProperty("Values", BindingFlags.Public | BindingFlags.Instance);
                                                                        if (valuesProp != null)
                                                                        {
                                                                                var values = valuesProp.GetValue(htmlCollection);
                                                                                if (values is System.Collections.IEnumerable en1 && !(values is string))
                                                                                        items = en1.Cast<object>();
                                                                        }

                                                                        // If not found, scan properties for an enumerable
                                                                        if (items == null)
                                                                        {
                                                                                foreach (var prop in htmlCollection.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                                                                                {
                                                                                        try
                                                                                        {
                                                                                                var val = prop.GetValue(htmlCollection);
                                                                                                if (val is System.Collections.IEnumerable en && !(val is string))
                                                                                                {
                                                                                                        items = en.Cast<object>();
                                                                                                        break;
                                                                                                }
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                                // ignore
                                                                                        }
                                                                                }
                                                                        }

                                                                        // If still not found, scan fields
                                                                        if (items == null)
                                                                        {
                                                                                foreach (var field in htmlCollection.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                                                                {
                                                                                        try
                                                                                        {
                                                                                                var val = field.GetValue(htmlCollection);
                                                                                                if (val is System.Collections.IEnumerable en && !(val is string))
                                                                                                {
                                                                                                        items = en.Cast<object>();
                                                                                                        break;
                                                                                                }
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                                // ignore
                                                                                        }
                                                                                }
                                                                        }

                                                                        if (items != null)
                                                                        {
                                                                                foreach (var htmlContent in items)
                                                                                {
                                                                                        cancellationToken.ThrowIfCancellationRequested();

                                                                                        var htmlContentType = htmlContent.GetType();
                                                                                        var contentTextProp = htmlContentType.GetProperty("Content", BindingFlags.Public | BindingFlags.Instance);
                                                                                        var fileNameProp = htmlContentType.GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance) ?? htmlContentType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);

                                                                                        var htmlString = contentTextProp?.GetValue(htmlContent) as string;
                                                                                        if (string.IsNullOrWhiteSpace(htmlString))
                                                                                                continue;

                                                                                        var fileName = fileNameProp?.GetValue(htmlContent) as string;
                                                                                        var text = HtmlToPlainText(htmlString);
                                                                                        var title = !string.IsNullOrWhiteSpace(fileName) ? Path.GetFileNameWithoutExtension(fileName) : "Section";
                                                                                        var startIndex = AppendSectionContent(builder, title, text);
                                                                                        sections.Add(new DocumentSection(title, startIndex, 0));
                                                                                }
                                                                        }
                                                                }
                                                        }
                                                }
                                        }
                                }
                                catch
                                {
                                        // ignore and continue
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
