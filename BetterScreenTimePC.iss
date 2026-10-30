; Inno Setup script for BetterScreenTimePC.
;
; ASSUMES this .iss file sits in the solution root, as a sibling of the
; BetterScreenTimePC\ and BlockerWatchdog\ project folders - adjust every
; "Source:" path below if your actual layout differs.
;
; Build BOTH projects in Release configuration before compiling this script -
; the Source paths below point at bin\Release, not bin\Debug.

#define MyAppName "BetterScreenTimePC"
#define MyAppVersion "1.0"
#define MyAppExeName "BetterScreenTimePC.exe"
#define MyWatchdogExeName "BlockerWatchdog.exe"

[Setup]
; Generate your own GUID (Tools > Generate GUID in the Inno Setup IDE, or any
; GUID generator) and use it here - this one is a placeholder and must be
; unique to your app so Windows can tell upgrades of THIS app apart from
; anything else.
AppId={{B7C1E2A4-5F3D-4E8A-9C2B-1A2B3C4D5E6F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
; Both exes require elevation to run, and installing into Program Files
; requires admin to write there anyway - the installer itself must run
; elevated for either of those to work.
PrivilegesRequired=admin
OutputBaseFilename={#MyAppName}-Setup
Compression=lzma
SolidCompression=yes
; Uncomment if your projects are built specifically for x64 rather than
; "Any CPU"/x86 - leave commented if unsure, since forcing this incorrectly
; will make the installer refuse to run on a mismatched architecture.
; ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "BetterScreenTimePC\bin\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "BlockerWatchdog\bin\Release\{#MyWatchdogExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Only included if you've added a custom tray icon - harmless if it doesn't
; exist, the line is just skipped. LoadTrayIcon() looks for this file next to
; the exe at runtime (AppDomain.CurrentDomain.BaseDirectory), which is exactly
; where DestDir: "{app}" puts it.
Source: "BetterScreenTimePC\bin\Release\tray_icon.ico"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
; Only BetterScreenTimePC.exe gets a shortcut - BlockerWatchdog.exe has no
; window at all and should never be launched directly by a person.
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Stop any already-running instance (e.g. upgrading over an existing
  // install) before files get copied - Windows can't overwrite an exe file
  // while it's loaded as a running process. Both at once, same reasoning as
  // the dev taskkill trick: killing only one just gets it relaunched by the
  // other within a couple of seconds.
  Exec('taskkill.exe', '/F /IM {#MyAppExeName} /IM {#MyWatchdogExeName}', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;
