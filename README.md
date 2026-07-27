# FSChecklist

<img src="assets/fschecklist-logo.png" alt="FSChecklist logo" width="160">

A voice-driven checklist assistant for Microsoft Flight Simulator 2024.

FSChecklist reproduces the aviation *challenge-and-response* flow: the copilot
reads each item, the pilot answers through the microphone, and the application
advances when the response matches the checklist configuration.

> This project is intended for flight simulation only. Do not use it in
> real-world aviation operations.

## Features

- English voice callouts and `en-US` speech recognition;
- JSON-based checklists;
- global `F9` shortcut, even while the simulator is in the foreground;
- visual list of pending, current, and completed items;
- configurable accepted responses, with exact response validation;
- audible negative feedback for rejected responses;
- manual item confirmation and checklist termination;
- automatic transition to the next configured checklist;
- Portuguese and English interface languages;
- selectable Windows input microphone;
- configurable global shortcut, including key combinations;
- Windows speech recognition with no external API key required;
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

Smart App Control is different from SmartScreen and does not provide an
individual app exception. Development builds signed with a local self-signed
certificate can still be blocked by Smart App Control.

If no ready-to-use version is available on the Releases page, follow the
[build instructions](#build-from-source).

## Requirements

- 64-bit Windows 10 or Windows 11;
- a microphone configured as the Windows input device;
- the **English (United States)** speech-recognition package installed in
  Windows;
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
  if requested by Windows.

Microsoft Flight Simulator does not need to be running to test the application.

## Configure speech recognition

On Windows 11:

1. Open **Settings → Time & language → Language & region**.
2. Add **English (United States)**.
3. Open its **Language options** and download **Basic speech recognition**.
4. Also download **Enhanced speech recognition**, when available.
5. Open **Settings → Privacy & security → Microphone**.
6. Allow microphone access for desktop applications.
7. Under **Privacy & security → Speech**, enable online speech recognition.

Installing only the text-to-speech voice is not enough. FSChecklist requires
the English speech-recognition component.

The names of these settings may be slightly different on Windows 10.

## How to use

1. Open `FSChecklist.exe`.
2. Select an aircraft and a checklist.
3. Click **INICIAR OU F9** or press `F9`.
4. Wait for the copilot to read the item.
5. Answer using one of the responses configured in the checklist JSON.
6. Follow the progress in the checklist panel.

The microphone remains open throughout the checklist, including while the
copilot is reading a callout. After the final item, the app announces the
completion, selects the next configured checklist, and turns the microphone
off. Press `F9` again to start the next checklist.

Available controls:

- **Check icon — Force check:** manually confirms the current item;
- **Stop icon — Finish:** ends the current checklist and selects the next one.

A missing or mismatched response keeps the current item pending. The app emits
a negative beep, says that the response was not confirmed, and displays the
recognized text so the pilot can retry.

## Settings

Select the gear button in the top-right corner to open **Settings**.

Available options:

- **Interface language:** switches all application text between Portuguese and
  English. Checklist content and spoken responses remain in English;
- **Input microphone:** lists available capture devices and makes the selected
  device the default Windows input microphone, which is the device used by
  Windows speech recognition;
- **Shortcut:** opens a small capture form. Press a key or combination such as
  `F10`, `Ctrl+F9`, or `Ctrl+Shift+F10`, then confirm it.

Settings are saved in:

```text
%LocalAppData%\FSChecklist\settings.json
```

The settings button is disabled while a checklist is running.

## Error messages

Handled errors and unexpected interface exceptions are displayed in a modal
dialog instead of being shown only in the status bar. The dialog contains the
error details and a single **Entendido** / **Understood** button.

## Troubleshooting

### The microphone does not recognize my voice

- confirm that the correct microphone is the Windows default input device;
- verify microphone permissions for desktop applications;
- install the English (United States) speech-recognition package;
- confirm that the status reports `en-US` recognition as ready;
- speak only after the status indicates that the microphone is listening.

### F9 does not work

- check whether the interface reports that global `F9` is active;
- close any other application that may be intercepting the key;
- use the **INICIAR OU F9** button as an alternative.

### The application does not open

- extract the `.zip` file before running the application;
- install the .NET 8 Desktop Runtime;
- do not remove the `checklists` folder;
- open an issue with a screenshot and the error message.

## Add checklists

Place `.json` files inside the `checklists` folder. Item content and order are
controlled exclusively by the file: the application does not invent, reorder,
or skip steps using AI.

Checklist that accepts only a global list of exact responses:

```json
{
  "aircraft": "Fenix A320",
  "language": "en-US",
  "rules": {
    "acceptAnyAnswer": false,
    "acceptedResponses": [
      "set",
      "check",
      "on",
      "off",
      "auto",
      "auto and set"
    ]
  },
  "checklists": [
    {
      "id": "before_start",
      "name": "Before Start",
      "next": "Start",
      "items": ["Parking Brake", "Navi Lights"]
    },
    {
      "id": "start",
      "name": "Start",
      "next": null,
      "items": ["Beacon", "APU"]
    }
  ]
}
```

Checklist with specific responses:

```json
{
  "schemaVersion": 1,
  "aircraft": "B777",
  "language": "en-US",
  "rules": { "acceptAnyAnswer": false },
  "checklists": [
    {
      "id": "before-start",
      "name": "Before Start",
      "completedCallout": "Before start checklist complete",
      "items": [
        {
          "id": "parking-brake",
          "callout": "Parking brake",
          "responses": ["set", "released"]
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
  --output .\.build-output
Copy-Item .\.build-output\FSChecklist.exe .\FSChecklist.exe -Force
```

The executable will be created at `FSChecklist.exe` in the repository root.
Keep the existing `checklists` folder next to it.

The `build.ps1` script is the maintainer's distribution workflow. It also signs
the executable, so it requires the Windows SDK and a local
`CN=FSChecklist Local` certificate.

## Privacy and safety

- audio is handled by Windows speech recognition and may use Microsoft's online
  speech service when online speech recognition is enabled;
- FSChecklist does not store recordings or send audio to its own server;
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
