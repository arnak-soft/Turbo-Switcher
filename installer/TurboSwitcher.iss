; Turbo Switcher — установщик (Inno Setup 6)
; Сборка: ISCC.exe /DMyAppVersion=1.0.0 installer\TurboSwitcher.iss

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#ifndef MyAppVersionInfo
  #define MyAppVersionInfo "0.0.0.0"
#endif

#define MyAppName "Turbo Switcher"
#define MyAppExe "TurboSwitch.exe"
#define MyAppPublisher "Turbo Switcher"
#define MyAppURL "https://github.com/arnak-soft/Turbo-Switcher"

[Setup]
AppId={{A7B3C9D1-E4F5-6789-ABCD-EF0123456789}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish
OutputBaseFilename=TurboSwitcher Setup {#MyAppVersion}
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExe}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Важно: не используем mutex приложения, иначе оно не сможет стартовать после установки.
AppMutex=Local\TurboSwitcher.Setup.v1
CloseApplications=yes
VersionInfoVersion={#MyAppVersionInfo}
VersionInfoProductVersion={#MyAppVersionInfo}
VersionInfoProductName={#MyAppName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "startup"; Description: "Запускать вместе с Windows"; GroupDescription: "Дополнительно:"
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительно:"; Flags: unchecked

[Files]
Source: "..\publish\self-contained\{#MyAppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon
Name: "{group}\Удалить {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{userstartup}\Turbo Switcher.cmd"
Type: files; Name: "{userstartup}\Typo Switcher.cmd"
Type: files; Name: "{userstartup}\TypoSwitch.cmd"

[Code]
function ConfigPath: String;
begin
  Result := ExpandConstant('{userappdata}\Turbo Switcher\config.json');
end;

function StartupCmdPath: String;
begin
  Result := ExpandConstant('{userstartup}\Turbo Switcher.cmd');
end;

function BoolToJson(Value: Boolean): String;
begin
  if Value then
    Result := 'true'
  else
    Result := 'false';
end;

procedure WriteDefaultConfig(RunAtStartup: Boolean);
var
  Path, Dir, Content: String;
begin
  Path := ConfigPath;
  Dir := ExtractFileDir(Path);
  if not DirExists(Dir) then
    ForceDirectories(Dir);

  Content :=
    '{' + #13#10 +
    '  "auto_switch": true,' + #13#10 +
    '  "sound": false,' + #13#10 +
    '  "sound_style": "windows",' + #13#10 +
    '  "min_word_length": 3,' + #13#10 +
    '  "exceptions": [],' + #13#10 +
    '  "ignored_processes": [],' + #13#10 +
    '  "run_at_startup": ' + BoolToJson(RunAtStartup) + ',' + #13#10 +
    '  "check_updates": true' + #13#10 +
    '}';

  SaveStringToFile(Path, Content, False);
end;

procedure UpdateRunAtStartupInConfig(RunAtStartup: Boolean);
var
  Path: String;
  Lines: TArrayOfString;
  I, P: Integer;
  Val: String;
  Found: Boolean;
begin
  Path := ConfigPath;
  Val := BoolToJson(RunAtStartup);
  Found := False;

  if not FileExists(Path) then
  begin
    WriteDefaultConfig(RunAtStartup);
    Exit;
  end;

  if LoadStringsFromFile(Path, Lines) then
  begin
    for I := 0 to GetArrayLength(Lines) - 1 do
    begin
      P := Pos('"run_at_startup"', Lines[I]);
      if P > 0 then
      begin
        if Pos(':', Lines[I]) > 0 then
          Lines[I] := '  "run_at_startup": ' + Val + ','
        else
          Lines[I] := '  "run_at_startup": ' + Val;
        Found := True;
        Break;
      end;
    end;

    if Found then
      SaveStringsToFile(Path, Lines, False)
    else
      WriteDefaultConfig(RunAtStartup);
  end
  else
    WriteDefaultConfig(RunAtStartup);
end;

procedure ApplyStartupShortcut(Enabled: Boolean);
var
  CmdPath, ExePath, Content: String;
begin
  CmdPath := StartupCmdPath;
  ExePath := ExpandConstant('{app}\{#MyAppExe}');

  if Enabled then
  begin
    Content := '@echo off' + #13#10 + 'start "" "' + ExePath + '"' + #13#10;
    SaveStringToFile(CmdPath, Content, False);
  end
  else if FileExists(CmdPath) then
    DeleteFile(CmdPath);
end;

procedure RemoveLegacyStartupShortcuts;
var
  Names: TArrayOfString;
  I: Integer;
  Path: String;
begin
  SetArrayLength(Names, 2);
  Names[0] := 'Typo Switcher.cmd';
  Names[1] := 'TypoSwitch.cmd';

  for I := 0 to 1 do
  begin
    Path := ExpandConstant('{userstartup}\') + Names[I];
    if FileExists(Path) then
      DeleteFile(Path);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  RunAtStartup: Boolean;
begin
  if CurStep = ssPostInstall then
  begin
    RunAtStartup := WizardIsTaskSelected('startup');
    UpdateRunAtStartupInConfig(RunAtStartup);
    RemoveLegacyStartupShortcuts;
    ApplyStartupShortcut(RunAtStartup);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if FileExists(StartupCmdPath) then
      DeleteFile(StartupCmdPath);
    RemoveLegacyStartupShortcuts;
  end;
end;
