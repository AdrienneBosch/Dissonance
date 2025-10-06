using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

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
                public async Task ReadDocumentAsync_WithValidEpubFile_ReturnsExtractedContent()
                {
                        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".epub");

                        try
                        {
                                await CreateSampleEpubAsync(tempFile);

                                var service = new DocumentReaderService();
                                var result = await service.ReadDocumentAsync(tempFile);

                                Assert.Equal(tempFile, result.FilePath);
                                Assert.Equal("Sample Heading\nThis is the first paragraph.\nSecond paragraph.", result.PlainText);
                                Assert.Equal(9, result.WordCount);
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

                private static async Task CreateSampleEpubAsync(string filePath)
                {
                        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
                        {
                                var containerEntry = archive.CreateEntry("META-INF/container.xml");
                                await using (var containerStream = containerEntry.Open())
                                await using (var writer = new StreamWriter(containerStream, Encoding.UTF8))
                                {
                                        await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                                "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">" +
                                                "<rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles>" +
                                                "</container>");
                                }

                                var packageEntry = archive.CreateEntry("OEBPS/content.opf");
                                await using (var packageStream = packageEntry.Open())
                                await using (var writer = new StreamWriter(packageStream, Encoding.UTF8))
                                {
                                        await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                                "<package version=\"3.0\" xmlns=\"http://www.idpf.org/2007/opf\">" +
                                                "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:title>Sample</dc:title></metadata>" +
                                                "<manifest>" +
                                                "<item id=\"chap1\" href=\"text/chapter1.xhtml\" media-type=\"application/xhtml+xml\"/>" +
                                                "</manifest>" +
                                                "<spine><itemref idref=\"chap1\"/></spine>" +
                                                "</package>");
                                }

                                var chapterEntry = archive.CreateEntry("OEBPS/text/chapter1.xhtml");
                                await using (var chapterStream = chapterEntry.Open())
                                await using (var writer = new StreamWriter(chapterStream, Encoding.UTF8))
                                {
                                        await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                                "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>Sample</title></head><body>" +
                                                "<h1>Sample Heading</h1>" +
                                                "<p>This is the first paragraph.</p>" +
                                                "<p>Second paragraph.</p>" +
                                                "</body></html>");
                                }
                        }
                }
        }
}
