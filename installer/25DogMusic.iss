; 25DogMusic Windows 安装包脚本（由 release 流水线通过 ISCC 编译）

#ifndef APP_VERSION
  #define APP_VERSION "0.0.0"
#endif

; 发布文件目录（流水线可覆盖，便于本地验证）
#ifndef PUBLISH_DIR
  #define PUBLISH_DIR "..\publish\windows-x64"
#endif

[Setup]
AppId={{5B0B6F1E-2C66-4E4E-9C0B-25D06A11B001}
AppName=25DogMusic
AppVersion={#APP_VERSION}
AppPublisher=refout
AppPublisherURL=https://github.com/refout/25DogMusic
AppSupportURL=https://github.com/refout/25DogMusic/issues
DefaultDirName={autopf}\25DogMusic
DefaultGroupName=25DogMusic
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=25DogMusic-{#APP_VERSION}-windows-x64-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\src\25DogMusic.Desktop\Assets\appicon.ico
UninstallDisplayIcon={app}\25DogMusic.Desktop.exe

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: unchecked

[Files]
Source: "{#PUBLISH_DIR}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\25DogMusic"; Filename: "{app}\25DogMusic.Desktop.exe"
Name: "{autodesktop}\25DogMusic"; Filename: "{app}\25DogMusic.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\25DogMusic.Desktop.exe"; Description: "运行 25DogMusic"; Flags: nowait postinstall skipifsilent
