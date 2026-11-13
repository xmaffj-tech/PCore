#define MyAppName "PCore"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "PCore"
#define MyAppExeName "PCore.exe"

[Setup]
SetupIconFile=..\PCore.App\PCore.ico
AppId={{8A3D3B3B-76B4-4C67-AC8D-83B06C9E1F24}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\PCore
DefaultGroupName=PCore
OutputDir=..\artifacts
OutputBaseFilename=PCore-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\PCore.App\bin\Release\PCore.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\PCore"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\PCore"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить PCore"; Flags: postinstall nowait skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ReleaseValue: Cardinal;
begin
  Result := True;
  if (not RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', ReleaseValue)) or
     (ReleaseValue < 528040) then
  begin
    MsgBox('.NET Framework 4.8 is required. Install it before installing PCore.', mbError, MB_OK);
    Result := False;
  end;
end;
