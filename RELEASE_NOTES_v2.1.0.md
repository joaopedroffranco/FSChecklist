# FSChecklist v2.1.0

This release improves custom checklist maintenance and makes invalid JSON
errors immediately visible to the user.

## Changes

- invalid checklist JSON now opens an error dialog when files are refreshed;
- startup JSON errors are displayed after the main window is ready;
- error messages retain the invalid filename and parser details;
- removed unused fields from the included Fenix A320 checklist JSON;
- simplified the documented JSON example to contain only effective fields;
- retained the folder and refresh workflow introduced in version 2.0.

## Installation

1. Download `FSChecklist-Setup-2.1.0-win-x64.exe`.
2. Run the installer.
3. Choose the installation folder and complete the installation.

## Updating

Run the `2.1.0` installer over an existing installation without uninstalling
the previous version. Application files are replaced while existing and
user-created JSON files in the `checklists` folder are preserved.

## Requirements

- 64-bit Windows 10 or Windows 11;
- microphone access for desktop applications;
- Windows English (United States) speech-recognition component;
- Microsoft Flight Simulator 2024 for SimConnect-powered automatic callouts.

No separate .NET installation or Microsoft Flight Simulator SDK is required.

## Safety

FSChecklist is for flight simulation only. Do not use it for real-world
aviation, navigation, or training.
