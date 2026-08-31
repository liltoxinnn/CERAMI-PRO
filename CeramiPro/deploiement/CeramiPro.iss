; ====================================================================
;  CeramiPro — installateur Windows
;
;  Construit CeramiPro_Setup.exe à partir du dossier de publication.
;  L'installateur pose le logiciel, crée les raccourcis, et laisse à
;  l'atelier le seul réglage qui lui soit propre : le mot de passe de
;  sa base de données.
;
;  Construction :  deploiement\construire-installateur.bat
; ====================================================================

#define NomApplication    "CeramiPro"
#define VersionApplication "1.0.0"
#define Editeur           "Atelier de céramique"
#define Executable        "CeramiPro.exe"

[Setup]
AppId={{7C3E1A64-9B52-4D18-A0F7-2E6B5C9D8A31}
AppName={#NomApplication}
AppVersion={#VersionApplication}
AppVerName={#NomApplication} {#VersionApplication}
AppPublisher={#Editeur}
DefaultDirName={autopf}\{#NomApplication}
DefaultGroupName={#NomApplication}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=CeramiPro_Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; L'application est publiée en 64 bits : elle ne s'installe pas ailleurs.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#Executable}

[Languages]
Name: "francais"; MessagesFile: "compiler:Languages\French.isl"

[CustomMessages]
francais.CreerRaccourciBureau=Créer un raccourci sur le &Bureau
francais.LancerApplication=Lancer {#NomApplication}
francais.ReglagesRestants=Le mot de passe de votre base de données PostgreSQL reste à renseigner dans le fichier « appsettings.Local.json » du dossier de données de CeramiPro. Le logiciel vous le rappellera au premier démarrage.

[Tasks]
Name: "raccourcibureau"; Description: "{cm:CreerRaccourciBureau}"; \
    GroupDescription: "Raccourcis :"

[Files]
; Tout le dossier de publication : l'exécutable et le moteur .NET, afin
; que l'atelier n'ait rien d'autre à installer.
Source: "publication\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Journaux, documents produits et sauvegardes, dans le profil de
; l'utilisateur : le dossier du programme reste en lecture seule.
Name: "{localappdata}\{#NomApplication}"
Name: "{localappdata}\{#NomApplication}\journaux"
Name: "{localappdata}\{#NomApplication}\sauvegardes"

[Icons]
Name: "{group}\{#NomApplication}"; Filename: "{app}\{#Executable}"
Name: "{group}\Désinstaller {#NomApplication}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#NomApplication}"; Filename: "{app}\{#Executable}"; \
    Tasks: raccourcibureau

[Run]
; Ouvrir le fichier de réglages tout de suite évite que l'atelier
; découvre le mot de passe manquant au premier lancement.
Filename: "notepad.exe"; Parameters: """{localappdata}\{#NomApplication}\appsettings.Local.json"""; \
    Description: "Renseigner le mot de passe de la base de données"; \
    Flags: postinstall skipifsilent unchecked

Filename: "{app}\{#Executable}"; Description: "{cm:LancerApplication}"; \
    Flags: nowait postinstall skipifsilent

[Code]
{ Le fichier de réglages locaux n'est jamais livré : il contient le mot de
  passe de la base. Il est créé à partir de l'exemple, dans le dossier de
  données de l'utilisateur — modifiable sans droits d'administrateur — et
  une seule fois, afin qu'une réinstallation n'efface pas celui de
  l'atelier. }
procedure CurStepChanged(EtapeCourante: TSetupStep);
var
  Local, Exemple: string;
begin
  if EtapeCourante <> ssPostInstall then
    Exit;

  Local := ExpandConstant('{localappdata}\{#NomApplication}\appsettings.Local.json');
  Exemple := ExpandConstant('{app}\appsettings.Local.exemple.json');

  if not FileExists(Local) and FileExists(Exemple) then
    FileCopy(Exemple, Local, False);
end;
