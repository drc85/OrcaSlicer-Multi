#define AppVersion "0.9.1"
[Setup]
AppId={{9624E509-A864-46AA-90C3-B637B1234E5F}
AppName=OrcaSlicer Multi Printer Tabs
AppVersion={#AppVersion}
AppPublisher=drc85
AppPublisherURL=https://github.com/drc85/OrcaSlicer-Multi
DefaultDirName={localappdata}\Programs\OrcaMultiPrinterTabs
DefaultGroupName=OrcaSlicer Multi Printer Tabs
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=dist
OutputBaseFilename=OrcaMultiPrinterTabs-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\OrcaSlicerMultiPrinterTabs.exe
DisableProgramGroupPage=yes

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "OrcaSlicerMultiPrinterTabs.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "OrcaSlicerMultiPrinterTabs.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "plugin\workspace_bridge.py"; DestDir: "{app}\plugin"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OrcaSlicer Multi Printer Tabs"; Filename: "{app}\OrcaSlicerMultiPrinterTabs.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\OrcaSlicer Multi Printer Tabs"; Filename: "{app}\OrcaSlicerMultiPrinterTabs.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\OrcaSlicerMultiPrinterTabs.exe"; Description: "Start OrcaSlicer Multi Printer Tabs"; Flags: nowait postinstall skipifsilent
