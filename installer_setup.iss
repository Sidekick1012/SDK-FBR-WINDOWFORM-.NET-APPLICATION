; Script generated for Sidekick E-Invoicing System
; See Inno Setup documentation: http://www.jrsoftware.org/ishelp/

#define AppName "Sidekick E-Invoicing System"
#define AppVersion "1.0.0"
#define AppPublisher "Sidekick"
#define AppExeName "Sidekick-E-Invoicing.exe"

; ProjectDir is passed at compile time via /DProjectDir="..." from build_installer.bat
; Fallback to the directory of this .iss file if not provided
#ifndef ProjectDir
  #define ProjectDir SourcePath
#endif

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{5A09F21E-A2E4-4279-AB30-67A7D7E3A3C4}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
; Output directory
OutputDir={#ProjectDir}\InstallerOutput
OutputBaseFilename=Sidekick_E_Invoicing_Setup
Compression=lzma
SolidCompression=yes
; Icon
SetupIconFile={#ProjectDir}\Sidekick-E-Invoicing\icon-256x256.ico
UninstallDisplayIcon={app}\{#AppExeName}
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main Executable
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\Sidekick-E-Invoicing.exe"; DestDir: "{app}"; Flags: ignoreversion

; Config file
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\Sidekick-E-Invoicing.exe.config"; DestDir: "{app}"; Flags: ignoreversion

; Database - installed into AppData so seller data persists and is not overwritten on updates
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\einvoice.db"; DestDir: "{userappdata}\SidekickEInvoicing"; Flags: ignoreversion onlyifdoesntexist

; Logo and images
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\*.png"; DestDir: "{app}"; Flags: ignoreversion

; DLL Dependencies
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; SQLite architecture subdirectories
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\x86\*"; DestDir: "{app}\x86"; Flags: ignoreversion recursesubdirs createallsubdirs

; Logs directory structure
Source: "{#ProjectDir}\Sidekick-E-Invoicing\bin\Release\Logs\*"; DestDir: "{app}\Logs"; Flags: ignoreversion recursesubdirs createallsubdirs onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
