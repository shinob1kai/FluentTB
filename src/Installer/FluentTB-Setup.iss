; FluentTB Installer Script for Inno Setup
; Download Inno Setup from: https://jrsoftware.org/isdl.php

#define MyAppName "FluentTB"
#define MyAppVersion "2026.3.1"
#define MyAppPublisher "Shinob1Kai"
#define MyAppURL "https://github.com/shinob1kai/FluentTB"
#define MyAppExeName "FluentTB.exe"

[Setup]
; App information
AppId={{8C3F5A2D-9E4B-4F1C-B6A8-7D2E9F0C1B3A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=.\Output
OutputBaseFilename=FluentTB-Setup-{#MyAppVersion}
SetupIconFile=..\FluentTB\res\FluentTB.ico
UninstallDisplayIcon={app}\FluentTB.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Run at Windows startup"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\FluentTB\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Check if app is running
  if CheckForMutexes('FluentTB') then
  begin
    if MsgBox('FluentTB is currently running. Please close it before continuing.', mbConfirmation, MB_OKCANCEL) = IDOK then
    begin
      // Try to stop the process
      Exec('taskkill', '/F /IM FluentTB.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end
    else
    begin
      Result := False;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Stop the process before uninstalling
    Exec('taskkill', '/F /IM FluentTB.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
