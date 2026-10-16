; Inno Setup 6 Script for NetSpeed Meter
; Creates a professional Windows Setup Wizard (.exe) like Brave, Chrome, VS Code

#define MyAppName "NetSpeed Meter"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "NetSpeed Meter"
#define MyAppURL "https://github.com/alok706175/NetSpeed-Meter"
#define MyAppExeName "NetSpeedMeter.exe"

[Setup]
; Unique App GUID
AppId={{5E3B8F2A-3D9C-4A72-8C4E-6A91B876F5E1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Installs for current user without UAC admin prompt (like Brave/VS Code User Installer),
; but can also be elevated if desired
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
OutputDir=setup
OutputBaseFilename=NetSpeedMeter-Setup
SetupIconFile=NetSpeedMeter\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=force
RestartApplications=no
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";
Name: "startwithwindows"; Description: "Launch NetSpeed Meter automatically when Windows starts"; GroupDescription: "Windows Startup:";

[Files]
Source: "publish\NetSpeedMeter\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NetSpeedMeter"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startwithwindows

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden; RunOnceId: "StopNetSpeedMeter"
