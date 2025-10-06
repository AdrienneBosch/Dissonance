using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
                        cancellationToken.ThrowIfCancellationRequested();

                        string content = extension.ToLowerInvariant() switch
                        {
                                ".txt" => await ReadTextFileAsync(filePath, cancellationToken).ConfigureAwait(false),
                                ".epub" => await ReadEpubFileAsync(filePath, cancellationToken).ConfigureAwait(false),
                                _ => throw new NotSupportedException($"The document type '{extension}' is not supported."),
                        };

                        cancellationToken.ThrowIfCancellationRequested();

                        return new DocumentReadResult(filePath, content ?? string.Empty);
                }

                private static async Task<string> ReadTextFileAsync(string filePath, CancellationToken cancellationToken)
                {
                        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                        return content ?? string.Empty;
                }

                private static async Task<string> ReadEpubFileAsync(string filePath, CancellationToken cancellationToken)
                {
                        using var archive = ZipFile.OpenRead(filePath);

                        cancellationToken.ThrowIfCancellationRequested();

                        var containerEntry = archive.GetEntry("META-INF/container.xml")
                                ?? throw new InvalidDataException("The EPUB file is missing its container manifest.");

                        var containerDocument = await LoadXmlAsync(containerEntry, cancellationToken).ConfigureAwait(false);
                        var rootFilePath = containerDocument
                                .Descendants()
                                .FirstOrDefault(e => e.Name.LocalName.Equals("rootfile", StringComparison.OrdinalIgnoreCase))
                                ?.Attribute("full-path")?.Value;

                        if (string.IsNullOrWhiteSpace(rootFilePath))
                                throw new InvalidDataException("The EPUB file does not specify a package document.");

                        cancellationToken.ThrowIfCancellationRequested();

                        rootFilePath = NormalizeEntryPath(rootFilePath);
                        var packageEntry = archive.GetEntry(rootFilePath)
                                ?? throw new InvalidDataException("The EPUB file references a missing package document.");

                        var packageDocument = await LoadXmlAsync(packageEntry, cancellationToken).ConfigureAwait(false);

                        var manifestItems = packageDocument
                                .Descendants()
                                .Where(e => e.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase))
                                .Select(e => new ManifestItem(
                                        e.Attribute("id")?.Value ?? string.Empty,
                                        NormalizeHref(e.Attribute("href")?.Value ?? string.Empty),
                                        e.Attribute("media-type")?.Value))
                                .Where(item => !string.IsNullOrEmpty(item.Id) && !string.IsNullOrEmpty(item.Href))
                                .ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

                        if (manifestItems.Count == 0)
                                throw new InvalidDataException("The EPUB file does not contain any manifest items.");

                        var spineOrder = packageDocument
                                .Descendants()
                                .Where(e => e.Name.LocalName.Equals("itemref", StringComparison.OrdinalIgnoreCase))
                                .Select(e => e.Attribute("idref")?.Value)
                                .Where(id => !string.IsNullOrEmpty(id))
                                .ToList();

                        var contentItems = new List<ManifestItem>();
                        foreach (var id in spineOrder)
                        {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (id != null && manifestItems.TryGetValue(id, out var item) && IsReadableMediaType(item.MediaType))
                                {
                                        contentItems.Add(item);
                                }
                        }

                        if (contentItems.Count == 0)
                        {
                                contentItems.AddRange(manifestItems.Values.Where(item => IsReadableMediaType(item.MediaType)));
                        }

                        if (contentItems.Count == 0)
                                throw new InvalidDataException("The EPUB file does not contain any readable content.");

                        var basePath = GetBasePath(rootFilePath);
                        var builder = new StringBuilder();

                        foreach (var item in contentItems)
                        {
                                cancellationToken.ThrowIfCancellationRequested();

                                var entryPath = CombineEntryPath(basePath, item.Href);
                                var entry = archive.GetEntry(entryPath);
                                if (entry == null)
                                        continue;

                                var mediaType = item.MediaType?.ToLowerInvariant();
                                if (string.Equals(mediaType, "text/plain", StringComparison.Ordinal))
                                {
                                        var text = await ReadZipEntryTextAsync(entry).ConfigureAwait(false);
                                        AppendContent(builder, text);
                                        continue;
                                }

                                var xhtmlDocument = await LoadXmlAsync(entry, cancellationToken, isHtml: true).ConfigureAwait(false);
                                var textContent = ExtractPlainText(xhtmlDocument.Root);
                                AppendContent(builder, textContent);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        return NormalizePlainText(builder.ToString());
                }

                private static async Task<XDocument> LoadXmlAsync(ZipArchiveEntry entry, CancellationToken cancellationToken, bool isHtml = false)
                {
                        using var stream = entry.Open();
                        var settings = new XmlReaderSettings
                        {
                                Async = true,
                                DtdProcessing = DtdProcessing.Ignore,
                                IgnoreComments = true,
                                IgnoreProcessingInstructions = true,
                                IgnoreWhitespace = isHtml,
                        };

                        using var reader = XmlReader.Create(stream, settings);
                        return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                }

                private static async Task<string> ReadZipEntryTextAsync(ZipArchiveEntry entry)
                {
                        using var stream = entry.Open();
                        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                        return content ?? string.Empty;
                }

                private static void AppendContent(StringBuilder builder, string content)
                {
                        if (string.IsNullOrWhiteSpace(content))
                                return;

                        if (builder.Length > 0)
                                builder.AppendLine();

                        builder.Append(content.Trim());
                }

                private static string ExtractPlainText(XElement? element)
                {
                        if (element == null)
                                return string.Empty;

                        var builder = new StringBuilder();
                        AppendNodeText(element, builder);
                        return builder.ToString();
                }

                private static void AppendNodeText(XNode node, StringBuilder builder)
                {
                        switch (node)
                        {
                                case XText textNode:
                                        var value = textNode.Value;
                                        if (!string.IsNullOrWhiteSpace(value))
                                        {
                                                builder.Append(value);
                                        }
                                        else
                                        {
                                                builder.Append(' ');
                                        }

                                        break;

                                case XElement elementNode:
                                        if (ShouldSkipElement(elementNode.Name.LocalName))
                                                return;

                                        if (string.Equals(elementNode.Name.LocalName, "br", StringComparison.OrdinalIgnoreCase))
                                                builder.Append('\n');

                                        foreach (var child in elementNode.Nodes())
                                        {
                                                AppendNodeText(child, builder);
                                        }

                                        if (IsBlockElement(elementNode.Name.LocalName))
                                                builder.Append('\n');

                                        break;
                        }
                }

                private static bool ShouldSkipElement(string localName)
                {
                        return localName.Equals("script", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("style", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("svg", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("nav", StringComparison.OrdinalIgnoreCase);
                }

                private static bool IsBlockElement(string localName)
                {
                        return localName.Equals("p", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("div", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("section", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("article", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("header", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("footer", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("li", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("ul", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("ol", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("h1", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("h2", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("h3", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("h4", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("h5", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("h6", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("tr", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("table", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("body", StringComparison.OrdinalIgnoreCase)
                                || localName.Equals("html", StringComparison.OrdinalIgnoreCase);
                }

                private static string NormalizePlainText(string text)
                {
                        if (string.IsNullOrWhiteSpace(text))
                                return string.Empty;

                        var builder = new StringBuilder();
                        var previousWasSpace = false;
                        var previousWasNewline = false;

                        foreach (var ch in text.Replace('\r', '\n'))
                        {
                                if (ch == '\n')
                                {
                                        if (!previousWasNewline && builder.Length > 0 && builder[builder.Length - 1] == ' ')
                                        {
                                                builder.Length -= 1;
                                        }

                                        if (!previousWasNewline)
                                        {
                                                builder.Append('\n');
                                        }

                                        previousWasNewline = true;
                                        previousWasSpace = false;
                                }
                                else if (char.IsWhiteSpace(ch))
                                {
                                        if (!previousWasSpace && !previousWasNewline)
                                        {
                                                if (builder.Length > 0)
                                                        builder.Append(' ');

                                                previousWasSpace = true;
                                        }
                                }
                                else
                                {
                                        builder.Append(ch);
                                        previousWasSpace = false;
                                        previousWasNewline = false;
                                }
                        }

                        return builder.ToString().Trim();
                }

                private static bool IsReadableMediaType(string? mediaType)
                {
                        if (string.IsNullOrWhiteSpace(mediaType))
                                return false;

                        mediaType = mediaType.ToLowerInvariant();
                        return mediaType.Contains("xhtml")
                                || mediaType.Contains("html")
                                || mediaType.Contains("text");
                }

                private static string NormalizeHref(string href)
                {
                        if (string.IsNullOrWhiteSpace(href))
                                return string.Empty;

                        href = href.Replace('\\', '/');
                        try
                        {
                                href = Uri.UnescapeDataString(href);
                        }
                        catch
                        {
                                // Ignore malformed escape sequences and fall back to the raw value.
                        }

                        return NormalizeEntryPath(href);
                }

                private static string NormalizeEntryPath(string path)
                {
                        if (string.IsNullOrWhiteSpace(path))
                                return string.Empty;

                        var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        var stack = new Stack<string>();

                        foreach (var segment in segments)
                        {
                                if (segment == ".")
                                        continue;

                                if (segment == "..")
                                {
                                        if (stack.Count > 0)
                                                stack.Pop();

                                        continue;
                                }

                                stack.Push(segment);
                        }

                        return string.Join('/', stack.Reverse());
                }

                private static string CombineEntryPath(string basePath, string relativePath)
                {
                        if (string.IsNullOrEmpty(basePath))
                                return NormalizeEntryPath(relativePath);

                        if (string.IsNullOrEmpty(relativePath))
                                return NormalizeEntryPath(basePath);

                        return NormalizeEntryPath(basePath.TrimEnd('/') + "/" + relativePath);
                }

                private static string GetBasePath(string path)
                {
                        if (string.IsNullOrWhiteSpace(path))
                                return string.Empty;

                        var index = path.LastIndexOf('/');
                        if (index < 0)
                                return string.Empty;

                        return path.Substring(0, index + 1);
                }

                private readonly struct ManifestItem
                {
                        public ManifestItem(string id, string href, string? mediaType)
                        {
                                Id = id;
                                Href = href;
                                MediaType = mediaType;
                        }

                        public string Id { get; }

                        public string Href { get; }

                        public string? MediaType { get; }
                }
        }
}
