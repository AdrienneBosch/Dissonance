using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using Dissonance.Tests.TestInfrastructure;
using Dissonance.Windows.Controls;

using Xunit;

namespace Dissonance.Tests.Windows.Controls
{
        public class HighlightingFlowDocumentScrollViewerTests
        {
                [WindowsFact]
                public void IncrementalHighlightUpdatesUsePointerCache()
                {
                        const int iterations = 5000;
                        const int highlightLength = 8;
                        const int documentLength = 200_000;

                        StaTestRunner.Run(() =>
                        {
                                WpfTestHelper.EnsureApplication();

                                var viewer = new HighlightingFlowDocumentScrollViewer();
                                var document = new FlowDocument();
                                var paragraph = new Paragraph(new Run(new string('a', documentLength)));
                                document.Blocks.Add(paragraph);

                                viewer.Document = document;
                                viewer.HighlightBrush = Brushes.Yellow;
                                viewer.HighlightLength = highlightLength;

                                var getPointerMethod = typeof(HighlightingFlowDocumentScrollViewer)
                                        .GetMethod("GetTextPointerAtOffset", BindingFlags.NonPublic | BindingFlags.Instance);
                                var getOffsetMethod = typeof(HighlightingFlowDocumentScrollViewer)
                                        .GetMethod("GetOffsetFromPointer", BindingFlags.NonPublic | BindingFlags.Instance);
                                Assert.NotNull(getPointerMethod);
                                Assert.NotNull(getOffsetMethod);

                                var stopwatch = Stopwatch.StartNew();

                                for (var i = 0; i < iterations; i++)
                                {
                                        viewer.HighlightStartIndex = i;

                                        var pointer = (TextPointer?)getPointerMethod!.Invoke(viewer, new object[] { document, i });
                                        Assert.NotNull(pointer);

                                        var offset = (int)getOffsetMethod!.Invoke(viewer, new object[] { document, pointer! });
                                        Assert.Equal(i, offset);
                                }

                                stopwatch.Stop();

                                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2),
                                        $"Incremental highlights took too long: {stopwatch.Elapsed}.");

                                var finalStartPointer = (TextPointer?)getPointerMethod!.Invoke(viewer, new object[] { document, iterations - 1 });
                                var finalEndPointer = (TextPointer?)getPointerMethod.Invoke(viewer, new object[] { document, iterations - 1 + highlightLength });
                                Assert.NotNull(finalStartPointer);
                                Assert.NotNull(finalEndPointer);

                                var highlightRange = new TextRange(finalStartPointer!, finalEndPointer!);
                                Assert.Equal(new string('a', highlightLength), highlightRange.Text);

                                viewer.HighlightLength = 0;

                                var cacheValidField = typeof(HighlightingFlowDocumentScrollViewer)
                                        .GetField("_cacheValid", BindingFlags.NonPublic | BindingFlags.Instance);
                                Assert.NotNull(cacheValidField);
                                Assert.False((bool)cacheValidField!.GetValue(viewer)!);

                                var highlightField = typeof(HighlightingFlowDocumentScrollViewer)
                                        .GetField("_currentHighlight", BindingFlags.NonPublic | BindingFlags.Instance);
                                Assert.NotNull(highlightField);
                                Assert.Null(highlightField!.GetValue(viewer));

                                var newDocument = new FlowDocument(new Paragraph(new Run("reset")));
                                viewer.Document = newDocument;

                                var cachedOffsetField = typeof(HighlightingFlowDocumentScrollViewer)
                                        .GetField("_cachedOffset", BindingFlags.NonPublic | BindingFlags.Instance);
                                Assert.NotNull(cachedOffsetField);
                                Assert.Equal(0, (int)cachedOffsetField!.GetValue(viewer)!);

                                var resetPointer = (TextPointer?)getPointerMethod.Invoke(viewer, new object[] { newDocument, 0 });
                                Assert.NotNull(resetPointer);
                                Assert.Equal(newDocument.ContentStart, resetPointer);
                        });
                }

                private sealed class TestHighlightingViewer : HighlightingFlowDocumentScrollViewer
                {
                        public Rect NextRect { get; set; } = new Rect(0, 0, 20, 20);
                        public int ScrollRequests { get; private set; }

                        protected override Rect GetHighlightCharacterRect(TextPointer pointer)
                        {
                                return NextRect;
                        }

                        protected override void ScheduleBringIntoView(Paragraph paragraph)
                        {
                                ScrollRequests++;
                                base.ScheduleBringIntoView(paragraph);
                        }
                }

                [WindowsFact]
                public void SuppressesBringIntoViewWhileUserScrolling()
                {
                        StaTestRunner.Run(() =>
                        {
                                WpfTestHelper.EnsureApplication();

                                var viewer = new TestHighlightingViewer
                                {
                                        HighlightBrush = Brushes.Yellow,
                                };

                                var document = new FlowDocument();
                                var paragraph = new Paragraph(new Run(new string('a', 200)));
                                document.Blocks.Add(paragraph);
                                viewer.Document = document;
                                viewer.HighlightStartIndex = 0;
                                viewer.HighlightLength = 10;
                                Assert.Equal(1, viewer.ScrollRequests);

                                var isUserScrollingField = typeof(HighlightingFlowDocumentScrollViewer)
                                        .GetField("_isUserScrolling", BindingFlags.NonPublic | BindingFlags.Instance);
                                Assert.NotNull(isUserScrollingField);
                                isUserScrollingField!.SetValue(viewer, true);

                                viewer.HighlightStartIndex = 5;
                                Assert.Equal(1, viewer.ScrollRequests);
                        });
                }
        }
}
