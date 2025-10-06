# Architecture Overview

This document summarizes the major projects, layers, and patterns that power Dissonance.

## Solution Layout

- **Dissonance** – WPF application project that contains the UI, view models, services, and managers.
- **Dissonance.Tests** – Test project focused on validating service and manager logic.

## Core Layers

| Layer | Description |
| --- | --- |
| UI (Views) | XAML views located in `Windows/` and `Resources/` deliver the interface. |
| ViewModels | `ViewModels/` contains MVVM bindings used by the views. |
| Services | `Services/` wraps platform integrations such as speech synthesis and clipboard access. |
| Managers | `Managers/` orchestrate higher-level workflows, including session history and configuration. |
| Infrastructure | `Infrastructure/` houses shared helpers such as dependency injection and configuration. |

## Data Flow

1. A clipboard update is detected by the `ClipboardService`.
2. The `HistoryManager` persists the new entry and notifies subscribers.
3. The corresponding view model updates observable properties.
4. The view reflects the change through data binding.

## Extending the Application

- Add new services to `Services/` and register them in the dependency injection container.
- Create or modify view models to expose new functionality.
- Update XAML views to present new interactions or data visualizations.

## Testing Strategy

- Unit tests live in `Dissonance.Tests` and target services and managers.
- Use mocking frameworks to simulate clipboard and speech synthesis behavior.
- Automate UI smoke tests with tools such as WinAppDriver for regression coverage.
