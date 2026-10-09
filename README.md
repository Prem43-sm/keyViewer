# KeyViewer

![KeyViewer app icon](./KV.png)

A lightweight Windows WPF utility that displays keyboard shortcuts, mouse clicks, and scroll actions near the cursor for screen recordings.

<img width="842" height="905" alt="image" src="https://github.com/user-attachments/assets/bb9d32dd-65a5-429b-83fe-f5089a2b4d3d" /> 
<img width="161" height="95" alt="image" src="https://github.com/user-attachments/assets/9acd368d-d1f5-4c0e-b0d0-588406188e89" />



## Requirements

- Windows 10 or 11 (64-bit)
- .NET 8 SDK to build from source

## Build and run

From this directory:

```powershell
dotnet build
dotnet run
```

Click **Start Overlay** to install the global input hooks. Click **Stop Overlay** to remove them. The settings window can be minimized while the overlay is active.

## Create a setup executable

Install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then run this PowerShell command from the project directory:

```powershell
.\installer\Build-Installer.ps1
```

The self-contained Windows x64 installer is created at `dist\KeyboardMouseOverlaySetup.exe`. Share that file with your friends; they do not need to install .NET. The installer creates a desktop shortcut and a Start menu shortcut, and offers to launch the app when installation finishes. It installs for the current Windows user and does not require administrator access.

## Current features

- Global keyboard monitoring for key presses and modifier combinations
- Left, right, and middle click indicators; double-click recognition; vertical wheel indicators
- A topmost, click-through overlay positioned near the cursor and kept within the cursor's monitor
- Adjustable text size, opacity, display duration, and signed X/Y cursor distance (from -200 to +200 pixels)
- Local JSON settings stored beside the application

The overlay observes input only. It does not synthesize input or store typed content. Input hooks are removed when monitoring is stopped or the application exits.

## Limitations

This is the initial implementation. It currently has a basic settings window and dark overlay style; profiles and advanced filtering are not implemented yet. Verify overlay capture behavior in the specific OBS capture mode used for a recording.
