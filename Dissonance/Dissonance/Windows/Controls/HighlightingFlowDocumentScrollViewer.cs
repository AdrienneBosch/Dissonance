using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Dissonance.Windows.Controls
{
        public class HighlightingFlowDocumentScrollViewer : FlowDocumentScrollViewer
        {
                public static readonly DependencyProperty HighlightStartIndexProperty =
                        DependencyProperty.Register(nameof(HighlightStartIndex), typeof(int), typeof(HighlightingFlowDocumentScrollViewer), new PropertyMetadata(0, OnHighlightChanged));

                public static readonly DependencyProperty HighlightLengthProperty =
                        DependencyProperty.Register(nameof(HighlightLength), typeof(int), typeof(HighlightingFlowDocumentScrollViewer), new PropertyMetadata(0, OnHighlightChanged));

                public static readonly DependencyProperty HighlightBrushProperty =
                        DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(HighlightingFlowDocumentScrollViewer), new PropertyMetadata(Brushes.Transparent, OnHighlightChanged));

                private TextRange? _currentHighlight;
                private int _appliedStartIndex = -1;
                private int _appliedLength;
                private Brush? _appliedBrush;

                public int HighlightStartIndex
                {
                        get => (int)GetValue(HighlightStartIndexProperty);
                        set => SetValue(HighlightStartIndexProperty, value);
                }

                public int HighlightLength
                {
                        get => (int)GetValue(HighlightLengthProperty);
                        set => SetValue(HighlightLengthProperty, value);
                }

                public Brush HighlightBrush
                {
                        get => (Brush)GetValue(HighlightBrushProperty);
                        set => SetValue(HighlightBrushProperty, value);
                }

                protected override void OnDocumentChanged(FlowDocument oldDocument, FlowDocument newDocument)
                {
                        base.OnDocumentChanged(oldDocument, newDocument);
                        ClearHighlight();
                        _appliedStartIndex = -1;
                        _appliedLength = 0;
                        _appliedBrush = null;
                        UpdateHighlight();
                }

                private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                {
                        if (d is HighlightingFlowDocumentScrollViewer viewer)
                        {
                                viewer.UpdateHighlight();
                        }
                }

                private void UpdateHighlight()
                {
                        var document = Document;
                        if (document == null)
                        {
                                ClearHighlight();
                                _appliedStartIndex = -1;
                                _appliedLength = 0;
                                _appliedBrush = null;
                                return;
                        }

                        var highlightLength = HighlightLength;
                        if (highlightLength <= 0)
                        {
                                ClearHighlight();
                                _appliedStartIndex = -1;
                                _appliedLength = 0;
                                _appliedBrush = HighlightBrush;
                                return;
                        }

                        var brush = HighlightBrush ?? Brushes.Transparent;
                        if (_appliedStartIndex == HighlightStartIndex && _appliedLength == highlightLength && Equals(_appliedBrush, brush))
                                return;

                        ClearHighlight();

                        var startPointer = GetTextPointerAtOffset(document, HighlightStartIndex);
                        var endPointer = GetTextPointerAtOffset(document, HighlightStartIndex + highlightLength);
                        if (startPointer == null || endPointer == null)
                                return;

                        var range = new TextRange(startPointer, endPointer);
                        range.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
                        _currentHighlight = range;
                        _appliedStartIndex = HighlightStartIndex;
                        _appliedLength = highlightLength;
                        _appliedBrush = brush;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                                try
                                {
                                        range.Start.Paragraph?.BringIntoView();
                                }
                                catch
                                {
                                }
                        }), DispatcherPriority.Background);
                }

                private void ClearHighlight()
                {
                        if (_currentHighlight != null)
                        {
                                try
                                {
                                        _currentHighlight.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
                                }
                                catch
                                {
                                }

                                _currentHighlight = null;
                        }
                }

                private static TextPointer? GetTextPointerAtOffset(FlowDocument document, int offset)
                {
                        if (document == null)
                                return null;

                        var navigator = document.ContentStart;
                        var remaining = Math.Max(0, offset);

                        while (navigator != null && navigator.CompareTo(document.ContentEnd) < 0)
                        {
                                if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                                {
                                        var textRun = navigator.GetTextInRun(LogicalDirection.Forward);
                                        if (textRun.Length == 0)
                                        {
                                                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                                                continue;
                                        }

                                        if (remaining <= textRun.Length)
                                        {
                                                return navigator.GetPositionAtOffset(remaining, LogicalDirection.Forward);
                                        }

                                        remaining -= textRun.Length;
                                        navigator = navigator.GetPositionAtOffset(textRun.Length, LogicalDirection.Forward);
                                }
                                else
                                {
                                        navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                                }
                        }

                        return document.ContentEnd;
                }
        }
}
