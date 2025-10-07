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

                public static readonly DependencyProperty SelectionStartIndexProperty =
                        DependencyProperty.Register(nameof(SelectionStartIndex), typeof(int), typeof(HighlightingFlowDocumentScrollViewer), new PropertyMetadata(0));

                public static readonly DependencyProperty SelectionLengthProperty =
                        DependencyProperty.Register(nameof(SelectionLength), typeof(int), typeof(HighlightingFlowDocumentScrollViewer), new PropertyMetadata(0));

                public static readonly DependencyProperty SelectedTextProperty =
                        DependencyProperty.Register(nameof(SelectedText), typeof(string), typeof(HighlightingFlowDocumentScrollViewer), new PropertyMetadata(string.Empty));

                private TextRange? _currentHighlight;
                private int _appliedStartIndex = -1;
                private int _appliedLength;
                private Brush? _appliedBrush;
                private bool _suppressSelectionPublishing;

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

                public int SelectionStartIndex
                {
                        get => (int)GetValue(SelectionStartIndexProperty);
                        private set => SetValue(SelectionStartIndexProperty, value);
                }

                public int SelectionLength
                {
                        get => (int)GetValue(SelectionLengthProperty);
                        private set => SetValue(SelectionLengthProperty, value);
                }

                public string SelectedText
                {
                        get => (string)GetValue(SelectedTextProperty);
                        private set => SetValue(SelectedTextProperty, value);
                }

                protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
                {
                        base.OnPropertyChanged(e);

                        if (e.Property == FlowDocumentScrollViewer.DocumentProperty)
                        {
                                ClearHighlight();
                                _appliedStartIndex = -1;
                                _appliedLength = 0;
                                _appliedBrush = null;
                                PublishSelectionRange(0, 0, string.Empty);
                                UpdateHighlight();
                        }
                }

                protected override void OnSelectionChanged(RoutedEventArgs args)
                {
                        base.OnSelectionChanged(args);

                        if (_suppressSelectionPublishing)
                                return;

                        var document = Document;
                        var selection = Selection;

                        if (document == null || selection == null)
                        {
                                PublishSelectionRange(0, 0, string.Empty);
                                return;
                        }

                        var start = GetOffsetFromPointer(document, selection.Start);
                        var end = GetOffsetFromPointer(document, selection.End);
                        var length = Math.Max(0, end - start);
                        var text = length > 0 ? new TextRange(selection.Start, selection.End).Text : string.Empty;

                        PublishSelectionRange(start, length, text ?? string.Empty);
                }

                private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                {
                        if (d is HighlightingFlowDocumentScrollViewer viewer)
                        {
                                viewer.UpdateHighlight();
                        }
                }

                private void PublishSelectionRange(int start, int length, string text)
                {
                        _suppressSelectionPublishing = true;
                        try
                        {
                                SelectionStartIndex = Math.Max(0, start);
                                SelectionLength = Math.Max(0, length);
                                SelectedText = length > 0 ? text : string.Empty;
                        }
                        finally
                        {
                                _suppressSelectionPublishing = false;
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

                private static int GetOffsetFromPointer(FlowDocument document, TextPointer pointer)
                {
                        if (document == null || pointer == null)
                                return 0;

                        var navigator = document.ContentStart;
                        var offset = 0;

                        while (navigator != null && navigator.CompareTo(pointer) < 0 && navigator.CompareTo(document.ContentEnd) < 0)
                        {
                                if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                                {
                                        var textRun = navigator.GetTextInRun(LogicalDirection.Forward);
                                        if (textRun.Length == 0)
                                        {
                                                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                                                continue;
                                        }

                                        var runEnd = navigator.GetPositionAtOffset(textRun.Length, LogicalDirection.Forward);
                                        if (runEnd == null)
                                                break;

                                        if (pointer.CompareTo(runEnd) <= 0)
                                        {
                                                offset += navigator.GetOffsetToPosition(pointer);
                                                return Math.Max(0, offset);
                                        }

                                        offset += textRun.Length;
                                        navigator = runEnd;
                                }
                                else
                                {
                                        navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                                }
                        }

                        return Math.Max(0, offset);
                }
        }
}
