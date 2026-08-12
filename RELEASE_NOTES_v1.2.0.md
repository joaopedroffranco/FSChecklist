# FSChecklist v1.2.0

This release introduces a Windows installer and makes custom checklist file
management easier while preserving the lightweight JSON-based workflow.

## Highlights

- Windows installer with a selectable destination folder;
- Start menu shortcut and optional desktop shortcut;
- application and `checklists` folder installed together;
- folder button to open checklist JSON files directly in Windows Explorer;
- refresh button to reload JSON files without restarting FSChecklist;
- existing and user-created checklist JSON files are preserved during updates;
- updated installation and upgrade documentation;
- Portuguese and English tooltips for the new controls.

## Installation

1. Download `FSChecklist-Setup-1.2.0-win-x64.exe`.
2. Run the installer.
3. Review the license and choose the installation folder.
4. Optionally create a desktop shortcut.
5. Open FSChecklist from the Start menu or desktop shortcut.

The default destination is `%LocalAppData%\Programs\FSChecklist`. A different
folder can be selected during installation. Choosing a protected folder such
as `Program Files` may require administrator permission when editing checklist
JSON files.

## Updating

Run the `1.2.0` installer over an existing installation without uninstalling
the previous version. The application files are replaced while JSON files in
the `checklists` folder are preserved.

## Custom checklists

Use the folder icon in FSChecklist to open the `checklists` directory. Manage
the JSON files in Windows Explorer or a text editor, then select the refresh
icon in FSChecklist to reload them.

## Requirements

- 64-bit Windows 10 or Windows 11;
- microphone access for desktop applications;
- Windows English (United States) speech-recognition component;
- Microsoft Flight Simulator 2024 for SimConnect-powered automatic callouts.

No separate .NET installation or Microsoft Flight Simulator SDK is required.

## Safety

FSChecklist is for flight simulation only. Do not use it for real-world
aviation, navigation, or training.
