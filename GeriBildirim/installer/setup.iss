; Geri Bildirim kurulum script'i
; Yeni sürüm çıkarırken yalnızca AppVersion değerini değiştirin.

#define AppName "Geri Bildirim"
#define AppVersion "1.0.1"
#define AppPublisher "Yasin Çınar"
#define AppExeName "GeriBildirim.exe"
#define PublishDir "..\publish"

[Setup]
; AppId sürümler arasında DEĞİŞMEMELİ; yeni sürüm eskisinin üzerine kurulur.
AppId={{8F3A2C71-5B4D-4E6A-9C1D-7A2E4B6F8D30}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\GeriBildirim
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
MinVersion=10.0.22000
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\feedback.ico
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=output
OutputBaseFilename=GeriBildirim_Setup_{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
; Program dosyaları her kurulumda güncellenir.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "appsettings.json,menu.json,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

; Ayar dosyaları yalnızca ilk kurulumda yazılır, güncellemede mevcut olan ezilmez.
Source: "{#PublishDir}\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "{#PublishDir}\menu.json"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{#AppName} Kaldır"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent