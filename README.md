# Keyboard Mouse Overlay

A lightweight Windows WPF utility that displays keyboard shortcuts, mouse clicks, and scroll actions near the cursor for screen recordings.

## Requirements

- Windows 10 or 11
- .NET 8 SDK

## Build and run

From this directory:

```powershell
dotnet build
dotnet run
```

Click **Start Overlay** to install the global input hooks. Click **Stop Overlay** to remove them. The settings window can be minimized while the overlay is active.

## Current features

- Global keyboard monitoring for key presses and modifier combinations
- Left, right, and middle click indicators; double-click recognition; vertical wheel indicators
- A topmost, click-through overlay positioned near the cursor and kept within the cursor's monitor
- Adjustable text size, opacity, and display duration
- Local JSON settings stored beside the application

The overlay observes input only. It does not synthesize input or store typed content. Input hooks are removed when monitoring is stopped or the application exits.

## Limitations

This is the initial implementation. It currently has a basic settings window and dark overlay style; profiles, advanced filtering, animation, and installer packaging are not implemented yet. Verify overlay capture behavior in the specific OBS capture mode used for a recording.
