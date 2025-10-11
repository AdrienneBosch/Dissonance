using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Dissonance.Windows.Controls
{
        public class HighlightingFlowDocumentScrollViewer : FlowDocumentScrollViewer
        {
                // Expose a SelectionChanged routed event so XAML can attach handlers.
                public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
                        nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HighlightingFlowDocumentScrollViewer));

                public event RoutedEventHandler SelectionChanged
                {
                        add => AddHandler(SelectionChangedEvent, value);
                        remove => RemoveHandler(SelectionChangedEvent, value);
                }

                private readonly DispatcherTimer _scrollActivityTimer;

                public HighlightingFlowDocumentScrollViewer()
                {
                        // FlowDocumentScrollViewer does not expose a SelectionChanged routed event.
                        // Attach to the TextSelection.Changed event when the control is loaded and
                        // detach when unloaded to avoid subscribing to a null Selection.
                        Loaded += OnLoaded;
                        Unloaded += OnUnloaded;

                        AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged), true);

                        _scrollActivityTimer = new DispatcherTimer
                        {
                                Interval = TimeSpan.FromMilliseconds(250)
                        };
                        _scrollActivityTimer.Tick += (_, _) =>
                        {
                                _scrollActivityTimer.Stop();
                                _isUserScrolling = false;
                        };
                }

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

                private const int SelectionTextPublishThreshold = 2048;

                private TextRange? _currentHighlight;
                private int _appliedStartIndex = -1;
                private int _appliedLength;
                private Brush? _appliedBrush;
                private bool _suppressSelectionPublishing;
                private TextSelection? _attachedSelection;

                private FlowDocument? _cachedDocument;
                private TextPointer? _cachedPointer;
                private int _cachedOffset;
                private bool _cacheValid;
                private ScrollViewer? _scrollViewer;
                private bool _isUserScrolling;
                private bool _pendingProgrammaticScroll;
                private double? _lastHighlightTop;

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
                                ResetPointerCache();
                                PublishSelectionRange(0, 0, string.Empty);

                                // Ensure we detach from any previous selection and attach to the new one if present
                                if (_attachedSelection != null)
                                {
                                        try { _attachedSelection.Changed -= HandleSelectionChanged; } catch { }
                                        _attachedSelection = null;
                                }

                                UpdateHighlight();

                                if (Selection != null)
                                {
                                        try { Selection.Changed += HandleSelectionChanged; } catch { }
                                        _attachedSelection = Selection;
                                }
                        }
                }

                private void HandleSelectionChanged(object? sender, EventArgs args)
                {
                        if (_suppressSelectionPublishing)
                                return;

                        var document = Document;
                        TextSelection? selection = Selection;

                        if (document == null || selection == null)
                        {
                                PublishSelectionRange(0, 0, string.Empty);
                                // Raise the routed SelectionChanged event so XAML handlers are notified
                                RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
                                return;
                        }

                        var start = GetOffsetFromPointer(document, selection.Start);
                        var end = GetOffsetFromPointer(document, selection.End);
                        var length = Math.Max(0, end - start);
                        string? text = null;

                        if (length > 0 && length <= SelectionTextPublishThreshold)
                        {
                                text = new TextRange(selection.Start, selection.End).Text;
                        }

                        PublishSelectionRange(start, length, text);

                        // Raise the routed SelectionChanged event so XAML handlers are notified
                        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
                }

                private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                {
                        if (d is HighlightingFlowDocumentScrollViewer viewer)
                        {
                                viewer.UpdateHighlight();
                        }
                }

                private void PublishSelectionRange(int start, int length, string? text)
                {
                        _suppressSelectionPublishing = true;
                        try
                        {
                                SelectionStartIndex = Math.Max(0, start);
                                SelectionLength = Math.Max(0, length);
                                SelectedText = length > 0 && !string.IsNullOrEmpty(text) ? text : string.Empty;
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
                                ResetPointerCache();
                                return;
                        }

                        var highlightLength = HighlightLength;
                        var previousAppliedStart = _appliedStartIndex;
                        if (highlightLength <= 0)
                        {
                                if (_cacheValid)
                                {
                                        var movedBackward = previousAppliedStart >= 0
                                                ? HighlightStartIndex < previousAppliedStart
                                                : HighlightStartIndex < _cachedOffset;

                                        if (movedBackward)
                                                ResetPointerCache();
                                }

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

                        var paragraph = range.Start.Paragraph;
                        if (paragraph == null)
                        {
                                _lastHighlightTop = null;
                                return;
                        }

                        var highlightRect = Rect.Empty;
                        try
                        {
                                highlightRect = GetHighlightCharacterRect(range.Start);
                        }
                        catch
                        {
                                highlightRect = Rect.Empty;
                        }

                        var viewport = Rect.Empty;
                        var highlightVisible = false;
                        var largeJump = !_lastHighlightTop.HasValue;

                        if (_scrollViewer != null)
                        {
                                viewport = new Rect(
                                        _scrollViewer.HorizontalOffset,
                                        _scrollViewer.VerticalOffset,
                                        _scrollViewer.ViewportWidth > 0 ? _scrollViewer.ViewportWidth : ActualWidth,
                                        _scrollViewer.ViewportHeight > 0 ? _scrollViewer.ViewportHeight : ActualHeight);

                                if (!highlightRect.IsEmpty)
                                {
                                        highlightVisible = viewport.IntersectsWith(highlightRect);

                                        if (_lastHighlightTop.HasValue)
                                        {
                                                var delta = Math.Abs(highlightRect.Top - _lastHighlightTop.Value);
                                                var viewportHeight = viewport.Height > 0 ? viewport.Height : ActualHeight;
                                                var threshold = viewportHeight > 0 ? viewportHeight * 0.75 : 48.0;
                                                if (threshold <= 0)
                                                        threshold = 48.0;
                                                largeJump = delta >= threshold;
                                        }
                                }
                        }

                        if (!highlightRect.IsEmpty)
                        {
                                _lastHighlightTop = highlightRect.Top;
                        }
                        else
                        {
                                _lastHighlightTop = null;
                        }

                        var shouldScroll = false;
                        if (!_isUserScrolling)
                        {
                                if (_scrollViewer == null)
                                {
                                        shouldScroll = true;
                                }
                                else if (!highlightVisible)
                                {
                                        shouldScroll = true;
                                }
                                else if (largeJump)
                                {
                                        shouldScroll = true;
                                }
                        }

#if DEBUG
                        Debug.WriteLine(
                                $"[HighlightingFlowDocumentScrollViewer] UpdateHighlight: visible={highlightVisible}, " +
                                $"userScrolling={_isUserScrolling}, largeJump={largeJump}, shouldScroll={shouldScroll}, " +
                                $"rect={highlightRect}, viewport={viewport}");
#endif

                        if (shouldScroll)
                        {
                                ScheduleBringIntoView(paragraph);
                        }
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

                        _lastHighlightTop = null;
                }

                private TextPointer? GetTextPointerAtOffset(FlowDocument document, int offset)
                {
                        if (document == null)
                                return null;

                        EnsurePointerCache(document);

                        var targetOffset = Math.Max(0, offset);

                        if (_cacheValid && _cachedPointer != null && targetOffset < _cachedOffset)
                        {
                                ResetPointerCache();
                                EnsurePointerCache(document);
                        }
                        var (navigator, currentOffset) = GetStartingPointer(document, targetOffset);
                        var pointer = AdvanceToOffset(document, navigator, currentOffset, targetOffset, out var newPointer, out var newOffset);

                        UpdatePointerCache(document, newPointer, newOffset);

                        return pointer;
                }

                private int GetOffsetFromPointer(FlowDocument document, TextPointer pointer)
                {
                        if (document == null || pointer == null)
                                return 0;

                        EnsurePointerCache(document);

                        if (_cacheValid && _cachedPointer != null && pointer != null && pointer.CompareTo(_cachedPointer) < 0)
                        {
                                ResetPointerCache();
                                EnsurePointerCache(document);
                        }

                        var (navigator, currentOffset) = GetStartingPointer(document, pointer);
                        var offset = AdvanceToPointer(document, navigator, currentOffset, pointer, out var newPointer, out var newOffset);

                        UpdatePointerCache(document, newPointer, newOffset);

                        return offset;
                }

                private void EnsurePointerCache(FlowDocument document)
                {
                        if (!ReferenceEquals(_cachedDocument, document))
                        {
                                _cachedDocument = document;
                                _cachedPointer = document.ContentStart;
                                _cachedOffset = 0;
                                _cacheValid = _cachedPointer != null;
                        }
                }

                private void ResetPointerCache()
                {
                        _cachedDocument = null;
                        _cachedPointer = null;
                        _cachedOffset = 0;
                        _cacheValid = false;
                }

                private (TextPointer navigator, int offset) GetStartingPointer(FlowDocument document, int targetOffset)
                {
                        if (_cacheValid && _cachedPointer != null && targetOffset >= _cachedOffset)
                                return (_cachedPointer, _cachedOffset);

                        return (document.ContentStart, 0);
                }

                private (TextPointer navigator, int offset) GetStartingPointer(FlowDocument document, TextPointer targetPointer)
                {
                        if (_cacheValid && _cachedPointer != null && targetPointer.CompareTo(_cachedPointer) >= 0)
                                return (_cachedPointer, _cachedOffset);

                        return (document.ContentStart, 0);
                }

                private static TextPointer AdvanceToOffset(FlowDocument document, TextPointer? navigator, int offset, int targetOffset, out TextPointer? newPointer, out int newOffset)
                {
                        if (navigator == null)
                        {
                                newPointer = document.ContentStart;
                                newOffset = 0;
                                return document.ContentStart;
                        }

                        var currentPointer = navigator;
                        var currentOffset = offset;

                        while (currentPointer != null && currentPointer.CompareTo(document.ContentEnd) < 0)
                        {
                                var context = currentPointer.GetPointerContext(LogicalDirection.Forward);
                                if (context == TextPointerContext.Text)
                                {
                                        var textRun = currentPointer.GetTextInRun(LogicalDirection.Forward);
                                        var runLength = textRun.Length;
                                        if (runLength == 0)
                                        {
                                                currentPointer = currentPointer.GetNextContextPosition(LogicalDirection.Forward);
                                                continue;
                                        }

                                        if (targetOffset <= currentOffset + runLength)
                                        {
                                                var advance = targetOffset - currentOffset;
                                                var position = currentPointer.GetPositionAtOffset(advance, LogicalDirection.Forward);
                                                newPointer = position ?? document.ContentEnd;
                                                newOffset = targetOffset;
                                                return position ?? document.ContentEnd;
                                        }

                                        currentOffset += runLength;
                                        currentPointer = currentPointer.GetPositionAtOffset(runLength, LogicalDirection.Forward);
                                }
                                else
                                {
                                        currentPointer = currentPointer.GetNextContextPosition(LogicalDirection.Forward);
                                }
                        }

                        newPointer = document.ContentEnd;
                        newOffset = currentOffset;
                        return document.ContentEnd;
                }

                private static int AdvanceToPointer(FlowDocument document, TextPointer? navigator, int offset, TextPointer targetPointer, out TextPointer? newPointer, out int newOffset)
                {
                        if (navigator == null)
                        {
                                newPointer = document.ContentStart;
                                newOffset = 0;
                                return 0;
                        }

                        var currentPointer = navigator;
                        var currentOffset = offset;

                        while (currentPointer != null && currentPointer.CompareTo(document.ContentEnd) < 0)
                        {
                                if (targetPointer.CompareTo(currentPointer) == 0)
                                {
                                        newPointer = currentPointer;
                                        newOffset = currentOffset;
                                        return currentOffset;
                                }

                                var context = currentPointer.GetPointerContext(LogicalDirection.Forward);
                                if (context == TextPointerContext.Text)
                                {
                                        var textRun = currentPointer.GetTextInRun(LogicalDirection.Forward);
                                        var runLength = textRun.Length;
                                        if (runLength == 0)
                                        {
                                                currentPointer = currentPointer.GetNextContextPosition(LogicalDirection.Forward);
                                                continue;
                                        }

                                        var runEnd = currentPointer.GetPositionAtOffset(runLength, LogicalDirection.Forward);
                                        if (runEnd == null)
                                                break;

                                        if (targetPointer.CompareTo(runEnd) <= 0)
                                        {
                                                var delta = currentPointer.GetOffsetToPosition(targetPointer);
                                                if (delta < 0)
                                                        delta = 0;

                                                var result = currentOffset + delta;
                                                newPointer = targetPointer;
                                                newOffset = result;
                                                return result;
                                        }

                                        currentOffset += runLength;
                                        currentPointer = runEnd;
                                }
                                else
                                {
                                        currentPointer = currentPointer.GetNextContextPosition(LogicalDirection.Forward);
                                }
                        }

                        newPointer = document.ContentEnd;
                        newOffset = currentOffset;
                        if (targetPointer.CompareTo(document.ContentEnd) >= 0)
                                return currentOffset;

                        return Math.Max(0, currentOffset);
                }

                private void UpdatePointerCache(FlowDocument document, TextPointer? pointer, int offset)
                {
                        _cachedDocument = document;

                        if (pointer == null)
                        {
                                _cachedPointer = document.ContentEnd;
                                _cachedOffset = Math.Max(0, offset);
                                _cacheValid = _cachedPointer != null;
                                return;
                        }

                        _cachedPointer = pointer;
                        _cachedOffset = Math.Max(0, offset);
                        _cacheValid = true;
                }

                private void OnLoaded(object? sender, RoutedEventArgs e)
                {
                        if (Selection != null && _attachedSelection != Selection)
                        {
                                try { Selection.Changed += HandleSelectionChanged; } catch { }
                                _attachedSelection = Selection;
                        }
                }

                private void OnUnloaded(object? sender, RoutedEventArgs e)
                {
                        if (_attachedSelection != null)
                        {
                                try { _attachedSelection.Changed -= HandleSelectionChanged; } catch { }
                                _attachedSelection = null;
                        }

                        _scrollActivityTimer.Stop();
                }

                private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
                {
                        if (e?.OriginalSource is ScrollViewer viewer)
                        {
                                _scrollViewer = viewer;

                                if (_pendingProgrammaticScroll)
                                        return;

                                if (Math.Abs(e.HorizontalChange) > double.Epsilon || Math.Abs(e.VerticalChange) > double.Epsilon)
                                {
                                        _isUserScrolling = true;
                                        _scrollActivityTimer.Stop();
                                        _scrollActivityTimer.Start();
                                }
                        }
                }

                protected virtual Rect GetHighlightCharacterRect(TextPointer pointer)
                {
                        return pointer.GetCharacterRect(LogicalDirection.Forward);
                }

                protected virtual void ScheduleBringIntoView(Paragraph paragraph)
                {
                        if (paragraph == null)
                                return;

                        _pendingProgrammaticScroll = true;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                                try
                                {
                                        paragraph.BringIntoView();
                                }
                                catch
                                {
                                }
                                finally
                                {
                                        _pendingProgrammaticScroll = false;
                                }
                        }), DispatcherPriority.Background);
                }
        }
}
