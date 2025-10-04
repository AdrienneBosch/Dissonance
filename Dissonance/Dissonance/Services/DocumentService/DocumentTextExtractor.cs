using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Xml.Linq;

namespace Dissonance.Services.DocumentService
{
        internal class DocumentTextExtractor : IDocumentTextExtractor
        {
                private static readonly IReadOnlyCollection<string> SupportedExtensions = new[] { ".txt", ".rtf", ".docx" };
                private static readonly XNamespace WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                public IReadOnlyCollection<string> SupportedFileExtensions => SupportedExtensions;

                public Task<string?> ExtractTextAsync ( string filePath, CancellationToken cancellationToken = default )
                {
                        if ( string.IsNullOrWhiteSpace ( filePath ) )
                                throw new ArgumentException ( "File path cannot be null or whitespace.", nameof ( filePath ) );

                        return Task.Run ( ( ) => ExtractTextInternal ( filePath, cancellationToken ), cancellationToken );
                }

                private static string? ExtractTextInternal ( string filePath, CancellationToken cancellationToken )
                {
                        cancellationToken.ThrowIfCancellationRequested ( );

                        var extension = Path.GetExtension ( filePath )?.ToLowerInvariant ( );
                        if ( extension == null || !SupportedExtensions.Contains ( extension ) )
                                throw new NotSupportedException ( $"Files with extension '{extension}' are not supported." );

                        return extension switch
                        {
                                ".txt" => ReadPlainText ( filePath, cancellationToken ),
                                ".rtf" => ReadRichText ( filePath, cancellationToken ),
                                ".docx" => ReadWordDocument ( filePath, cancellationToken ),
                                _ => throw new NotSupportedException ( $"Files with extension '{extension}' are not supported." ),
                        };
                }

                private static string? ReadPlainText ( string filePath, CancellationToken cancellationToken )
                {
                        using var stream = new FileStream ( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
                        using var reader = new StreamReader ( stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true );
                        var text = reader.ReadToEnd ( );
                        cancellationToken.ThrowIfCancellationRequested ( );
                        return Sanitize ( text );
                }

                private static string? ReadRichText ( string filePath, CancellationToken cancellationToken )
                {
                        string? result = null;
                        Exception? failure = null;

                        var thread = new Thread ( ( ) =>
                        {
                                try
                                {
                                        using var stream = new FileStream ( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
                                        var flowDocument = new FlowDocument ( );
                                        var textRange = new TextRange ( flowDocument.ContentStart, flowDocument.ContentEnd );
                                        textRange.Load ( stream, DataFormats.Rtf );
                                        result = textRange.Text;
                                }
                                catch ( Exception ex )
                                {
                                        failure = ex;
                                }
                        } )
                        {
                                IsBackground = true
                        };

                        thread.SetApartmentState ( ApartmentState.STA );
                        thread.Start ( );
                        thread.Join ( );

                        cancellationToken.ThrowIfCancellationRequested ( );

                        if ( failure != null )
                                throw failure;

                        return Sanitize ( result );
                }

                private static string? ReadWordDocument ( string filePath, CancellationToken cancellationToken )
                {
                        using var stream = new FileStream ( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
                        using var archive = new ZipArchive ( stream, ZipArchiveMode.Read, leaveOpen: false );
                        var entry = archive.GetEntry ( "word/document.xml" );
                        if ( entry == null )
                                return null;

                        using var entryStream = entry.Open ( );
                        var document = XDocument.Load ( entryStream, LoadOptions.PreserveWhitespace );
                        cancellationToken.ThrowIfCancellationRequested ( );

                        var sb = new StringBuilder ( );
                        foreach ( var paragraph in document.Descendants ( WordNamespace + "p" ) )
                        {
                                var paragraphText = string.Concat ( paragraph
                                        .Descendants ( WordNamespace + "t" )
                                        .Select ( t => t.Value ) );

                                if ( string.IsNullOrWhiteSpace ( paragraphText ) )
                                        continue;

                                if ( sb.Length > 0 )
                                        sb.Append ( ' ' );

                                sb.Append ( paragraphText );
                        }

                        return Sanitize ( sb.ToString ( ) );
                }

                private static string? Sanitize ( string? text )
                {
                        if ( string.IsNullOrWhiteSpace ( text ) )
                                return null;

                        var sanitized = Regex.Replace ( text, "\\s+", " " ).Trim ( );
                        return string.IsNullOrEmpty ( sanitized ) ? null : sanitized;
                }
        }
}
