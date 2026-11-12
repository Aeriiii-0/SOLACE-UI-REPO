; Script generated for Solace Installer
; Compiled with Inno Setup

#define MyAppName "Solace"
#define MyAppVersion "0.0.9-beta"
#define MyAppPublisher "CSWDO - City of Biñan"
#define MyAppExeName "SOLUM-UI.exe"

[Setup]
AppId={{D37F27B8-9A8A-4D78-BC84-5C32E909B11A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Solace
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; "PrivilegesRequired=lowest" installs to %LocalAppData% so standard users never need an Administrator password!
PrivilegesRequired=lowest
OutputDir=..\Output
OutputBaseFilename=SolaceSetup
SetupIconFile=..\Assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Copy all files and folders from the Release build
Source: "..\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu Shortcut
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
; Automatically create Desktop Shortcut
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
; Option to launch the application immediately after install finishes
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
