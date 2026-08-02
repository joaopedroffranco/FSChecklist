# FSChecklist v1.0

The first stable release of FSChecklist Community.

## Highlights

- Challenge-and-response voice checklists;
- English callouts and `en-US` speech recognition;
- Portuguese and English interface languages;
- configurable global shortcut and microphone selection;
- JSON-based aircraft checklists;
- automatic SimConnect reconnection status;
- automatic takeoff, altitude, and landing callouts.

## Installation

1. Download `FSChecklist-v1.0-win-x64.zip`.
2. Extract the complete archive to a folder.
3. Run `FSChecklist.exe`.
4. Install Windows English (United States) speech recognition.

Keep the `checklists` folder next to the executable.

## SimConnect

The official x64 `SimConnect.dll` is included beside the executable. Open
Microsoft Flight Simulator 2024 and FSChecklist will connect automatically.
The application remains fully usable without the simulator; SimConnect only
enables simulator-aware automatic flight callouts.

## Notes

- voice recognition requires the Windows `en-US` speech component;
- checklist responses are limited to the phrases configured in the aircraft JSON;
- aircraft compatibility and automatic callouts will continue to expand;
- Windows may display a SmartScreen warning for the downloaded executable.

## Safety

FSChecklist is for flight simulation only. Do not use it for real-world
aviation, navigation, or training.

Report problems at:

https://github.com/joaopedroffranco/FSChecklist/issues
