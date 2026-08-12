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
- configurable global shortcut, with `F9` as the default, even while the
  simulator is in the foreground;
- visual list of pending, current, and completed items;
- configurable accepted responses, with exact response validation;
- audible negative feedback for rejected responses;
- manual item confirmation and checklist termination;
- automatic transition to the next configured checklist;
- Portuguese and English interface languages;
- selectable Windows input microphone;
- support for key combinations as shortcuts;
- Windows speech recognition with no external API key required;
- support for multiple aircraft and checklists;
- automatic SimConnect connection and reconnection status for Microsoft
  Flight Simulator 2024;
- SimConnect integration for automatic flight callouts, including examples
  such as `V one`, `Positive climb`, altitude crossings, spoilers, and reverse
  status.

## Download and install

### Ready-to-use version

1. Open the
   [Releases](https://github.com/joaopedroffranco/FSChecklist/releases) page.
2. Select the latest version.
3. Under **Assets**, download `FSChecklist-Setup-<version>-win-x64.exe`.
4. Run the installer.
5. Review the license and choose the installation folder. The default is
   `%LocalAppData%\Programs\FSChecklist`, but another writable folder can be
   selected.
6. Optionally create a desktop shortcut and complete the installation.
7. Open FSChecklist from the Start menu or the desktop shortcut.

The installer places the `checklists` folder beside `FSChecklist.exe`. If a
protected destination such as `Program Files` is selected, Windows may require
administrator permission to edit checklist files. If Windows SmartScreen
appears, verify that the installer came from this repository before selecting
**More info → Run anyway**.

Smart App Control is different from SmartScreen and does not provide an
individual app exception. Development builds signed with a local self-signed
certificate can still be blocked by Smart App Control.

### Update an existing installation

Download the installer for the newer version and run it without uninstalling
the current version first. The installer recognizes the existing installation,
keeps its destination, replaces the application files, and preserves existing
JSON files in the `checklists` folder.

For example, to update from `1.1.0` to `1.2.0`, run
`FSChecklist-Setup-1.2.0-win-x64.exe` over the existing installation. Use the
same Windows user account that installed the previous version.

## Requirements

- 64-bit Windows 10 or Windows 11;
- a microphone configured as the Windows input device;
- the **English (United States)** speech-recognition package installed in
  Windows;
- no separate .NET installation is required;
- the official x64 `SimConnect.dll` is included beside `FSChecklist.exe`.

The application remains usable when Microsoft Flight Simulator is not running.
It reconnects through SimConnect automatically every five seconds.

## SimConnect

The official x64 `SimConnect.dll` is included in the release package. Open
Microsoft Flight Simulator 2024 and FSChecklist connects automatically. The
connection indicator at the top of the checklist panel reports whether the
simulator is connected. Voice checklists continue to work when SimConnect is
unavailable because the integration is used only for simulator-aware automatic
flight callouts.

### Automatic flight callouts

While SimConnect is connected, FSChecklist monitors the user aircraft and
speaks these callouts in English:

- `One hundred knots` during an armed takeoff roll;
- `V one` when a valid V1 supplied by the aircraft is crossed;
- `Positive climb` after liftoff with a positive vertical speed;
- `Ten thousand` when indicated altitude crosses 10,000 ft in either
  direction;
- `Two thousand` when radio altitude crosses 2,000 ft during descent;
- `Spoilers`, `Reverse green` or `No reverse` after touchdown;
- `Sixty knots` during the landing roll;
- `Manual brake` when pilot braking is detected with autobrake inactive.

Takeoff mode is armed only while accelerating on the ground above 30 knots
with both engines at takeoff power. Landing-roll callouts are armed only after
the application has observed the aircraft airborne, which prevents false
callouts when the application starts while taxiing.

V1 is never estimated. The initial integration reads the common
`L:AIRLINER_V1_SPEED` value and the Fenix
`L:FNX2PLD_speedV1` value. If neither aircraft value contains a plausible
speed, the V1 callout is skipped.

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
3. Click **START** or use the configured shortcut (`F9` by default).
4. Wait for the copilot to read the item.
5. Answer using one of the responses configured in the checklist JSON.
6. Follow the progress in the checklist panel.

The microphone remains open throughout the checklist, including while the
copilot is reading a callout. After the final item, the app announces the
completion, selects the next configured checklist, and turns the microphone
off. Use the configured shortcut again to start the next checklist.

Available controls:

- **Folder icon:** opens the `checklists` folder beside the application in
  Windows Explorer. Add, edit, rename, or remove JSON files there;
- **Refresh icon:** reloads the JSON files after they are changed in Windows
  Explorer;
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

### The shortcut does not work

- remember that `F9` is only the default and check the shortcut selected in
  **Settings**;
- check whether the interface reports that the configured global shortcut is
  active;
- close any other application that may be intercepting the key;
- use the **START** button as an alternative.

### The application does not open

- reinstall the latest version over the existing installation;
- confirm that antivirus or Windows security did not quarantine the
  application;
- no separate .NET installation is required because the installer includes a
  self-contained build;
- open an issue with a screenshot and the error message.

## Add checklists

Place `.json` files inside the `checklists` folder. Item content and order are
controlled exclusively by the file: the application does not invent, reorder,
or skip steps using AI.

Use the folder icon in the application to open the correct location. After
adding, editing, renaming, or deleting JSON files, return to FSChecklist and
select the refresh icon. Existing checklist files are not overwritten when the
application is updated and are preserved by the uninstaller.

The supported contract uses a global list of exact responses and checklist
items represented by strings:

```json
{
  "aircraft": "Fenix A320",
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

See `checklists/a320.json` for a complete example.

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

FSChecklist is being developed as a commercial product, so its complete
distribution is not intended to be 100% open source. Issues and pull requests
for the public parts of the project are still welcome.

When reporting a problem, include:

- Windows version;
- aircraft and checklist used;
- error message;
- steps to reproduce;
- a screenshot, when possible.

## License

FSChecklist is currently distributed as freeware for personal,
non-commercial flight-simulation use. See [LICENSE.txt](LICENSE.txt) for the
complete terms. The FSChecklist name and logo remain protected.
