# Dissonance Accessibility Tool

Dissonance is a Windows desktop narrator that focuses on making clipboard text and plain-text documents easier to review with speech synthesis. It provides keyboard-driven controls, theme switching, and configurable voices so low-vision users can stay in their flow without switching tools.

## Key Features

- **Clipboard Reader** – Capture the current selection or monitor the clipboard automatically, then read it aloud with text-to-speech controls.
- **Document Reader** – Load `.txt` files, follow along with synchronized highlighting, and resume from your last saved position.
- **Customizable Voices** – Choose an installed Windows voice, adjust speaking rate and volume, and preview the sound before applying it across the app.
- **Hotkey Management** – Configure the global shortcut that copies and narrates clipboard content, including whether it should auto-read newly captured text.
- **Accessible Theming & Feedback** – Toggle light or dark themes and review recent status announcements to track background operations.

## Getting Started

### Prerequisites

- Windows 10 or later
- .NET Desktop Runtime 8.0 (for running) or .NET SDK 8.0 (for development)

### Installation

1. Clone the repository.
2. Restore the solution dependencies: `dotnet restore`.
3. Build the application: `dotnet build Dissonance/Dissonance.sln`.
4. Run the app: `dotnet run --project Dissonance/Dissonance/Dissonance/Dissonance.csproj`.

## Usage Overview

1. Launch Dissonance.
2. Copy text from another application or trigger the global hotkey to capture a selection.
3. Switch to the Clipboard Reader view to listen immediately or adjust auto-read behavior.
4. Open the Document Reader to review saved `.txt` files with playback and highlighting controls.
5. Use the Reader Settings panel to fine-tune voice, rate, and volume.

## Architecture Highlights

- **Services** encapsulate integrations such as speech synthesis, clipboard monitoring, theme management, and configuration storage.
- **Managers** coordinate domain logic like clipboard access and application startup routines.
- **ViewModels** implement MVVM bindings for the WPF views, ensuring testability and separation of concerns.

## Accessibility Considerations

- Supports screen readers and keyboard navigation throughout the interface.
- Adjustable voice parameters and color themes improve comfort for low-vision users.
- Status announcements provide audible and visible confirmation of background actions.

## Contributing

1. Fork the repository and create a feature branch.
2. Run the test suite with `dotnet test`.
3. Submit a pull request with a detailed description of your changes.

## License

This project is released under the MIT License. See the [`LICENSE`](LICENSE) file for details.

## Additional Resources

For more detailed walkthroughs and design notes, see the [project wiki](./wiki/Home.md).
