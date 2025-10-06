# Clipboard Reader

The Clipboard Reader is the heart of Dissonance. It continuously monitors the system clipboard and surfaces content in an accessible format.

![Clipboard Reader – dark mode](../Dissonance/Assets/Wiki/clipboard_reader_page_dark_mode.png)

## Workflow

1. Copy text in any Windows application.
2. Dissonance captures the clipboard contents and adds it to the session history.
3. The Clipboard Reader view displays the captured text with formatting controls and speech playback options.

## Key Components

- **ClipboardService** – Listens for clipboard updates and publishes new entries.
- **HistoryManager** – Stores clipboard snapshots and provides filtering and retrieval utilities.
- **ClipboardReaderViewModel** – Bridges the UI and services, exposing properties for the WPF views.

## Tips for Power Users

- Enable continuous reading to automatically trigger text-to-speech when new content arrives.
- Use keyboard shortcuts to navigate between history items without leaving the reader view.
- Adjust the voice, rate, and volume to match the context you are working in.

## Troubleshooting

If clipboard monitoring stops working:

- Confirm no other application has locked the clipboard.
- Restart Dissonance to re-initialize the listener hooks.
- Check the application logs for exceptions or warnings.
