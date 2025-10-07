using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Text;

using Dissonance.Services.DocumentReader;

using Xunit;

namespace Dissonance.Tests.Services
{
        public class DocumentReaderServiceTests
        {
                [Fact]
                public async Task ReadDocumentAsync_WithValidTextFile_ReturnsContent()
                {
                        var sampleText = "Hello world" + Environment.NewLine + "Second line";
                        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
                        await File.WriteAllTextAsync(tempFile, sampleText);

                        try
                        {
                                var service = new DocumentReaderService();
                                var result = await service.ReadDocumentAsync(tempFile);

                                Assert.Equal(tempFile, result.FilePath);
                                Assert.Equal(sampleText, result.PlainText);
                                Assert.Equal(4, result.WordCount);
                                Assert.Equal(sampleText.Length, result.CharacterCount);
                        }
                        finally
                        {
                                if (File.Exists(tempFile))
                                {
                                        File.Delete(tempFile);
                                }
                        }
                }

                [Fact]
                public async Task ReadDocumentAsync_WithMissingFile_Throws()
                {
                        var service = new DocumentReaderService();
                        await Assert.ThrowsAsync<FileNotFoundException>(() => service.ReadDocumentAsync("missing.txt"));
                }

                [Fact]
                public async Task ReadDocumentAsync_WithUnsupportedExtension_Throws()
                {
                        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".rtf");
                        await File.WriteAllTextAsync(tempFile, "sample");

                        try
                        {
                                var service = new DocumentReaderService();
                                await Assert.ThrowsAsync<NotSupportedException>(() => service.ReadDocumentAsync(tempFile));
                        }
                        finally
                        {
                                if (File.Exists(tempFile))
                                {
                                        File.Delete(tempFile);
                                }
                        }
                }

                [Fact]
                public async Task ReadDocumentAsync_WithSimpleEpub_ReturnsSectionsAndPlainText()
                {
                        var tempFile = CreateSampleEpub();

                        try
                        {
                                var service = new DocumentReaderService();
                                var result = await service.ReadDocumentAsync(tempFile);

                                Assert.Equal(tempFile, result.FilePath);
                                Assert.NotNull(result.PlainText);
                                Assert.Contains("First paragraph.", result.PlainText, StringComparison.Ordinal);
                                Assert.Single(result.Sections);
                                var section = result.Sections[0];
                                Assert.Equal("chapter1", section.Title, StringComparer.OrdinalIgnoreCase);
                                Assert.Equal(0, section.StartCharacterIndex);
                        }
                        finally
                        {
                                if (File.Exists(tempFile))
                                {
                                        File.Delete(tempFile);
                                }
                        }
                }

                private static string CreateSampleEpub()
                {
                        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".epub");
                        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                        {
                                var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                                using (var writer = new StreamWriter(mimetypeEntry.Open(), Encoding.ASCII, leaveOpen: false))
                                {
                                        writer.Write("application/epub+zip");
                                }

                                var containerEntry = archive.CreateEntry("META-INF/container.xml", CompressionLevel.Optimal);
                                using (var writer = new StreamWriter(containerEntry.Open(), Encoding.UTF8, leaveOpen: false))
                                {
                                        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n  <rootfiles>\n    <rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>\n  </rootfiles>\n</container>");
                                }

                                var opfEntry = archive.CreateEntry("OEBPS/content.opf", CompressionLevel.Optimal);
                                using (var writer = new StreamWriter(opfEntry.Open(), Encoding.UTF8, leaveOpen: false))
                                {
                                        writer.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"BookId\">\n  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n    <dc:identifier id=\"BookId\">urn:uuid:{Guid.NewGuid()}</dc:identifier>\n    <dc:title>Sample EPUB</dc:title>\n    <meta property=\"dcterms:modified\">2020-01-01T00:00:00Z</meta>\n  </metadata>\n  <manifest>\n    <item id=\"chap1\" href=\"chapter1.xhtml\" media-type=\"application/xhtml+xml\" />\n  </manifest>\n  <spine>\n    <itemref idref=\"chap1\" />\n  </spine>\n</package>");
                                }

                                var htmlEntry = archive.CreateEntry("OEBPS/chapter1.xhtml", CompressionLevel.Optimal);
                                using (var writer = new StreamWriter(htmlEntry.Open(), Encoding.UTF8, leaveOpen: false))
                                {
                                        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<html xmlns=\"http://www.w3.org/1999/xhtml\">\n  <head>\n    <title>Chapter One</title>\n  </head>\n  <body>\n    <h1>Chapter One</h1>\n    <p>First paragraph.</p>\n    <p>Second paragraph!</p>\n  </body>\n</html>");
                                }
                        }

                        return tempFile;
                }
        }
}
