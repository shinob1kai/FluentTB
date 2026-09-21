; Wrap the same MSI so EXE and MSI share one installation and upgrade identity.
[Setup]
AppName=FluentTB
AppVersion=@VERSION@
AppPublisher=Shinob1Kai
CreateAppDir=no
Uninstallable=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=@OUTPUT@
OutputBaseFilename=FluentTB-Public-@VERSION@-x64-Setup
SetupIconFile=@ICON@
WizardStyle=modern
Compression=lzma2
[Files]
Source: "@MSI@"; DestDir: "{tmp}"; Flags: deleteafterinstall
[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var ResultCode: Integer;
begin
  if CurStep = ssPostInstall then begin
    if not Exec(ExpandConstant('{sys}\msiexec.exe'), '/i "' + ExpandConstant('{tmp}\FluentTB-Public-@VERSION@-x64.msi') + '"', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      RaiseException('Unable to start Windows Installer.');
    if (ResultCode <> 0) and (ResultCode <> 3010) then
      RaiseException('Windows Installer did not complete. Exit code: ' + IntToStr(ResultCode));
  end;
end;
