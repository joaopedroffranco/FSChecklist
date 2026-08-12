#ifndef AppVersion
  #define AppVersion "1.2.0"
#endif
#ifndef SourceRoot
  #define SourceRoot ".."
#endif

[Setup]
AppId={{D1A39083-08DD-4CF1-97E1-7CA700935C94}
AppName=FSChecklist
AppVersion={#AppVersion}
AppPublisher=FSChecklist
DefaultDirName={localappdata}\Programs\FSChecklist
DefaultGroupName=FSChecklist
AllowNoIcons=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#SourceRoot}\release
OutputBaseFilename=FSChecklist-Setup-{#AppVersion}-win-x64
SetupIconFile={#SourceRoot}\assets\fschecklist.ico
UninstallDisplayIcon={app}\FSChecklist.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
LicenseFile={#SourceRoot}\LICENSE.txt
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceRoot}\FSChecklist.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\SimConnect.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\checklists\*.json"; DestDir: "{app}\checklists"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "{#SourceRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\PRIVACY.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\RELEASE_NOTES_v1.2.0.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FSChecklist"; Filename: "{app}\FSChecklist.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\FSChecklist"; Filename: "{app}\FSChecklist.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\FSChecklist.exe"; Description: "{cm:LaunchProgram,FSChecklist}"; Flags: nowait postinstall skipifsilent
