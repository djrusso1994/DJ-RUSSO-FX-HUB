#define MyAppName "DJ RUSSO FX HUB"
#ifndef MyAppVersion
  #define MyAppVersion "0.8.46"
#endif
#define MyAppExeName "DJRussoFXHub.exe"

[Setup]
AppId={{7C67103A-4F3A-4C63-95E4-7DFAF4A81212}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher=DJ RUSSO
DefaultDirName={autopf}\DJ RUSSO FX HUB
DefaultGroupName=DJ RUSSO FX HUB
DisableProgramGroupPage=yes
OutputDir=..\installer-output
OutputBaseFilename=DJ_RUSSO_FX_HUB_Setup_v{#MyAppVersion}
SetupIconFile=..\Assets\hub.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=yes
LanguageDetectionMethod=none

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\DJ RUSSO FX HUB"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\DJ RUSSO FX HUB"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,DJ RUSSO FX HUB}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\initial-language.txt"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  HubLanguage: String;
begin
  if CurStep = ssPostInstall then
  begin
    HubLanguage := 'en';
    if ActiveLanguage = 'italian' then HubLanguage := 'it';
    if ActiveLanguage = 'spanish' then HubLanguage := 'es';
    if ActiveLanguage = 'french' then HubLanguage := 'fr';
    if ActiveLanguage = 'german' then HubLanguage := 'de';
    if not SaveStringToFile(ExpandConstant('{app}\initial-language.txt'), HubLanguage, False) then
      Log('Could not write FX HUB initial language preference.');
  end;
end;
