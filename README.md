# FSChecklist

<img src="assets/fschecklist-logo.png" alt="FSChecklist logo" width="160">

A voice-driven checklist assistant for Microsoft Flight Simulator 2024.

FSChecklist reproduces the aviation *challenge-and-response* flow: the copilot
reads each item, the pilot answers through the microphone, and the application
advances when the response matches the checklist configuration.

> This project is intended for flight simulation only. Do not use it in
> real-world aviation operations.

## Features

- Brazilian Portuguese voice callouts and speech recognition;
- JSON-based checklists;
- global `F9` shortcut, even while the simulator is in the foreground;
- visual list of pending, current, and completed items;
- manual item confirmation;
- local voice processing on the computer;
- support for multiple aircraft and checklists.

## Download and install

### Ready-to-use version

1. Open the
   [Releases](https://github.com/joaopedroffranco/FSChecklist/releases) page.
2. Select the latest version.
3. Under **Assets**, download the file provided for Windows.
4. If the download is a `.zip` file, extract all its contents into a folder.
5. Run `FSChecklist.exe`.

Keep the `checklists` folder next to the executable. If Windows SmartScreen
appears, verify that the file came from this repository before selecting
**More info → Run anyway**.

If no ready-to-use version is available on the Releases page, follow the
[build instructions](#build-from-source).

## Requirements

- 64-bit Windows 10 or Windows 11;
- a microphone configured as the Windows input device;
- the **Portuguese (Brazil)** speech package installed in Windows;
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
  if requested by Windows.

Microsoft Flight Simulator does not need to be running to test the application.

## Configure speech recognition

On Windows 11:

1. Open **Settings → Time & language → Language & region**.
2. Add **Portuguese (Brazil)** and install its speech feature.
3. Open **Settings → Privacy & security → Microphone**.
4. Allow microphone access for desktop applications.
5. Under **Privacy & security → Speech**, enable speech recognition.

The names of these settings may be slightly different on Windows 10.

## How to use

1. Open `FSChecklist.exe`.
2. Select an aircraft and a checklist.
3. Click **START** or press `F9`.
4. Wait for the copilot to read the item.
5. Answer the callout through the microphone.
6. Follow the progress in the checklist panel.

The microphone remains active while the checklist is running. The copilot's
voice is ignored while a callout is being played.

Available controls:

- **✓ — Force check:** manually confirms the current item;
- **■ — Finish:** stops the checklist without confirming the remaining items.

A missing, uncertain, or mismatched response keeps the current item pending.

## Troubleshooting

### The microphone does not recognize my voice

- confirm that the correct microphone is the Windows default input device;
- verify microphone permissions for desktop applications;
- install the Portuguese (Brazil) speech package;
- speak only after the status indicates that the microphone is listening.

### F9 does not work

- check whether the interface reports that global `F9` is active;
- close any other application that may be intercepting the key;
- use the **START** button as an alternative.

### The application does not open

- extract the `.zip` file before running the application;
- install the .NET 8 Desktop Runtime;
- do not remove the `checklists` folder;
- open an issue with a screenshot and the error message.

## Add checklists

Place `.json` files inside the `checklists` folder. Item content and order are
controlled exclusively by the file: the application does not invent, reorder,
or skip steps using AI.

Checklist that accepts any recognized response:

```json
{
  "aircraft": "Fenix A320",
  "rules": { "acceptAnyAnswer": true },
  "checklists": [
    {
      "id": "before_start",
      "name": "Before Start",
      "items": ["Parking Brake", "Navi Lights"]
    }
  ]
}
```

Checklist with specific responses:

```json
{
  "schemaVersion": 1,
  "aircraft": "B777",
  "language": "pt-BR",
  "checklists": [
    {
      "id": "before-start",
      "name": "Before Start",
      "completedCallout": "Before start checklist complete",
      "items": [
        {
          "id": "parking-brake",
          "callout": "Parking brake",
          "responses": ["set", "acionado"]
        }
      ]
    }
  ]
}
```

See `checklists/a320.json` for a complete example.

## Build from source

Development requirements:

- [Git](https://git-scm.com/);
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0);
- 64-bit Windows 10 or Windows 11.

Clone and publish the project:

```powershell
git clone https://github.com/joaopedroffranco/FSChecklist.git
cd FSChecklist
dotnet restore .\src\FSChecklist.csproj --configfile .\NuGet.Config
dotnet publish .\src\FSChecklist.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output .\dist
New-Item .\dist\checklists -ItemType Directory -Force
Copy-Item .\checklists\*.json .\dist\checklists -Force
```

The executable will be created at `dist\FSChecklist.exe`.

The `build.ps1` script is the maintainer's distribution workflow. It also signs
the executable, so it requires the Windows SDK and a local
`CN=FSChecklist Local` certificate.

## Privacy and safety

- audio is processed locally by the Windows speech engine;
- unrecognized responses do not advance the checklist;
- the interface always displays the current item and progress;
- checklist content comes from JSON files;
- the application does not replace official aviation procedures or
  documentation.

## Contributing

Issues and pull requests are welcome. When reporting a problem, include:

- Windows version;
- aircraft and checklist used;
- error message;
- steps to reproduce;
- a screenshot, when possible.
