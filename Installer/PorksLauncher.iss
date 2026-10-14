; Compile through Build-Installer.ps1, which supplies the published app and prerequisite paths.
#ifndef PublishDir
  #error PublishDir must point to the Release publish output.
#endif
#ifndef PrerequisiteDir
  #error PrerequisiteDir must point to the verified .NET Framework installer.
#endif
#ifndef AppVersion
  #define AppVersion GetFileVersion(PublishDir + "\PorksLauncher.exe")
#endif
#ifndef InstallerOutputDir
  #define InstallerOutputDir "..\bin\releases"
#endif

[Setup]
AppId={{B433FB75-75A1-40AF-A543-E33D11332D28}
AppName=PorksLauncher
AppVersion={#AppVersion}
AppPublisher=Adam-OGithub
AppPublisherURL=https://github.com/Adam-OGithub/PorksLauncher
AppSupportURL=https://github.com/Adam-OGithub/PorksLauncher/issues
DefaultDirName={localappdata}\Programs\PorksLauncher
DefaultGroupName=PorksLauncher
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
LicenseFile=..\LICENSE
OutputDir={#InstallerOutputDir}
OutputBaseFilename=PorksLauncher-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\images\PorksLauncher.ico
UninstallDisplayIcon={app}\PorksLauncher.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PrerequisiteDir}\NDP48-x86-x64-AllOS-ENU.exe"; Flags: dontcopy

[Icons]
Name: "{userprograms}\PorksLauncher"; Filename: "{app}\PorksLauncher.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\PorksLauncher"; Filename: "{app}\PorksLauncher.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\PorksLauncher.exe"; Description: "Launch PorksLauncher"; Flags: nowait postinstall skipifsilent; Check: CanLaunchApplication

[Code]
var
  FrameworkRestartRequired: Boolean;

function CanLaunchApplication: Boolean;
begin
  Result := not FrameworkRestartRequired;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
begin
  Result := '';
  if IsDotNetInstalled(net472, 0) then
    exit;

  WizardForm.StatusLabel.Caption := 'Installing Microsoft .NET Framework 4.8...';
  ExtractTemporaryFile('NDP48-x86-x64-AllOS-ENU.exe');
  if not ShellExec('runas', ExpandConstant('{tmp}\NDP48-x86-x64-AllOS-ENU.exe'),
    '/passive /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ExitCode) then
  begin
    Result := 'The .NET Framework installation could not start. Allow its administrator prompt, then try again.';
    exit;
  end;

  if (ExitCode = 3010) or (ExitCode = 1641) then
  begin
    FrameworkRestartRequired := True;
    NeedsRestart := True;
  end
  else if ExitCode <> 0 then
    Result := Format('.NET Framework installation failed (code %d). Restart Windows and try setup again.', [ExitCode])
  else if not IsDotNetInstalled(net472, 0) then
    Result := '.NET Framework is not available yet. Restart Windows and try setup again.';
end;

function NeedRestart: Boolean;
begin
  Result := FrameworkRestartRequired;
end;
