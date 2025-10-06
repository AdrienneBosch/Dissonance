# Getting Started

This guide covers the basics of cloning, building, and running Dissonance on a Windows development machine.

![Clipboard Reader – light mode](../Dissonance/Assets/Wiki/clipboard_reader_page_light_mode.png)

## Prerequisites

- Windows 10 or newer
- .NET SDK 8.0 (includes the necessary desktop workloads)
- Git

## Clone the Repository

```powershell
git clone https://github.com/<your-account>/Dissonance.git
cd Dissonance
```

## Restore and Build

```powershell
dotnet restore Dissonance.sln
dotnet build Dissonance.sln
```

The build output is located in `Dissonance/bin/<Configuration>/<TargetFramework>/`.

## Run the Application

```powershell
dotnet run --project Dissonance/Dissonance.csproj
```

Running with `dotnet run` launches the WPF app using the current build configuration.

## Run Tests

```powershell
dotnet test
```

The solution includes unit tests in the `Dissonance.Tests` project. Make sure the suite passes before submitting a pull request.

## Next Steps

- Review the [Architecture Overview](./Architecture.md) for a deep dive into the solution structure.
- Explore the [Theming](./Theming.md) guide to learn how to customize the UI experience.
