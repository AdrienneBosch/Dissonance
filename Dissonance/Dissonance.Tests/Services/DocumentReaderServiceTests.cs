using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;

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
                                Assert.IsType<FlowDocument>(result.Document);
                                var paragraphs = result.Document.Blocks.OfType<Paragraph>().ToList();
                                Assert.Equal(2, paragraphs.Count);
                                Assert.Equal("Hello world", new TextRange(paragraphs[0].ContentStart, paragraphs[0].ContentEnd).Text.TrimEnd('\r', '\n'));
                                Assert.Equal("Second line", new TextRange(paragraphs[1].ContentStart, paragraphs[1].ContentEnd).Text.TrimEnd('\r', '\n'));
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
        }
}
