# FSChecklist v1.0-beta

The first public beta of the free FSChecklist Community release.

## Highlights

- Challenge-and-response voice checklists;
- English callouts and `en-US` speech recognition;
- Portuguese and English interface languages;
- configurable global shortcut and microphone selection;
- JSON-based aircraft checklists;
- automatic SimConnect reconnection status;
- automatic takeoff, altitude, and landing callouts.

## Installation

1. Download `FSChecklist-v1.0-beta-win-x64.zip`.
2. Extract the complete archive to a folder.
3. Run `FSChecklist.exe`.
4. Install Windows English (United States) speech recognition.

Keep the `checklists` folder next to the executable.

## SimConnect

The official x64 `SimConnect.dll` is included beside the executable. Open
Microsoft Flight Simulator 2024 and FSChecklist will connect automatically.
The application remains fully usable without the simulator; SimConnect only
enables simulator-aware automatic flight callouts.

## Beta limitations

- voice recognition requires the Windows `en-US` speech component;
- accepted checklist responses use exact configured phrases;
- aircraft compatibility and automatic callouts are still being expanded;
- the executable uses a local development signature and Windows may display a
  SmartScreen warning.

## Safety

FSChecklist is for flight simulation only. Do not use it for real-world
aviation, navigation, or training.

Report problems at:

https://github.com/joaopedroffranco/FSChecklist/issues
