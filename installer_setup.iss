; Script generated for HCR E-Invoicing System
; See Inno Setup documentation: http://www.jrsoftware.org/ishelp/

#define AppName "HCR E-Invoicing System"
#define AppVersion "1.0.0"
#define AppPublisher "HCR"
#define AppExeName "HCR-E-INVOICING-SYSTEM.exe"

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
OutputBaseFilename=HCR_E_Invoicing_System_Setup
Compression=lzma
SolidCompression=yes
; Icon
SetupIconFile={#ProjectDir}\HCR-E-INVOICING-SYSTEM\icon-256x256.ico
UninstallDisplayIcon={app}\{#AppExeName}
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main Executable
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\HCR-E-INVOICING-SYSTEM.exe"; DestDir: "{app}"; Flags: ignoreversion

; Config file
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\HCR-E-INVOICING-SYSTEM.exe.config"; DestDir: "{app}"; Flags: ignoreversion

; Database - installed into AppData so seller data persists and is not overwritten on updates
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\einvoice.db"; DestDir: "{userappdata}\HCREInvoicing"; Flags: ignoreversion onlyifdoesntexist

; Logo and images
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\*.png"; DestDir: "{app}"; Flags: ignoreversion

; DLL Dependencies
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; SQLite architecture subdirectories
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\x86\*"; DestDir: "{app}\x86"; Flags: ignoreversion recursesubdirs createallsubdirs

; Logs directory structure
Source: "{#ProjectDir}\HCR-E-INVOICING-SYSTEM\bin\Release\Logs\*"; DestDir: "{app}\Logs"; Flags: ignoreversion recursesubdirs createallsubdirs onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
