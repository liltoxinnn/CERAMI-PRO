@echo off
rem ===================================================================
rem  CeramiPro — construction de CeramiPro_Setup.exe
rem
rem  Demande Inno Setup 6, gratuit :
rem      winget install JRSoftware.InnoSetup
rem
rem  Usage :  deploiement\construire-installateur.bat
rem ===================================================================
setlocal

set RACINE=%~dp0..
set SORTIE=%RACINE%\deploiement\publication
set INNO="%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"

if not exist "%SORTIE%\CeramiPro.exe" (
    echo.
    echo  L'executable est introuvable.
    echo  Lancez d'abord :  deploiement\publier.bat
    exit /b 1
)

if not exist %INNO% (
    echo.
    echo  Inno Setup 6 est introuvable.
    echo  Installez-le puis relancez :
    echo      winget install JRSoftware.InnoSetup
    exit /b 1
)

%INNO% "%RACINE%\deploiement\CeramiPro.iss"

if errorlevel 1 (
    echo.
    echo  La construction de l'installateur a echoue.
    exit /b 1
)

echo.
echo  Installateur pret : %RACINE%\deploiement\CeramiPro_Setup.exe
echo.

endlocal
