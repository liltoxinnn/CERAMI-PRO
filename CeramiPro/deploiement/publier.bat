@echo off
rem ===================================================================
rem  CeramiPro — production de l'exécutable Windows
rem
rem  Produit un dossier autonome contenant CeramiPro.exe et tout ce
rem  dont il a besoin : l'atelier n'a pas à installer .NET.
rem
rem  Usage :  deploiement\publier.bat
rem ===================================================================
setlocal

set RACINE=%~dp0..
set SORTIE=%RACINE%\deploiement\publication

echo.
echo  CeramiPro — publication
echo  =======================
echo.

rem Un dossier de publication ancien laisserait des fichiers d'une
rem version précédente à côté de la nouvelle.
if exist "%SORTIE%" (
    echo  Nettoyage du dossier de publication...
    rmdir /s /q "%SORTIE%"
)

dotnet publish "%RACINE%\src\CeramiPro.App\CeramiPro.App.csproj" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%SORTIE%" ^
    -p:PublishSingleFile=false ^
    -p:DebugType=none

if errorlevel 1 (
    echo.
    echo  La publication a echoue.
    exit /b 1
)

rem Le mot de passe de la base est propre à chaque ordinateur : le
rem fichier local ne doit jamais partir avec l'installateur.
if exist "%SORTIE%\appsettings.Local.json" del "%SORTIE%\appsettings.Local.json"

copy /y "%RACINE%\src\CeramiPro.App\appsettings.Local.exemple.json" "%SORTIE%\" >nul

echo.
echo  Publication terminee.
echo  Executable : %SORTIE%\CeramiPro.exe
echo.
echo  Pour construire l'installateur, lancez ensuite :
echo      deploiement\construire-installateur.bat
echo.

endlocal
