@echo off
setlocal
echo ========================================================
echo       BUILDING SOLACE RELEASE, INSTALLER & ZIP
echo ========================================================

:: 1. Compile Release build with MSBuild
echo.
echo [1/3] Compiling Release binaries with MSBuild...
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "%~dp0..\SOLUM-UI.csproj" /t:Build /p:Configuration=Release /v:minimal
if %ERRORLEVEL% neq 0 (
    echo Error: MSBuild compilation failed!
    pause
    exit /b %ERRORLEVEL%
)

:: 2. Compile Inno Setup Installer
echo.
echo [2/3] Generating SolaceSetup.exe with Inno Setup...
"C:\Users\Renzo\AppData\Local\Programs\Inno Setup 7\ISCC.exe" "%~dp0installer.iss"
if %ERRORLEVEL% neq 0 (
    echo Error: Inno Setup compilation failed!
    pause
    exit /b %ERRORLEVEL%
)

:: 3. Generate Release ZIP Archive
echo.
echo [3/3] Creating Release ZIP package...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%~dp0..\bin\Release\*' -DestinationPath '%~dp0..\Output\Solace-v0.0.6-beta.zip' -Force"
copy /y "%~dp0..\Output\Solace-v0.0.6-beta.zip" "%~dp0..\Output\SOLUM-UI-v0.0.6-beta.zip" >nul
copy /y "%~dp0..\Output\Solace-v0.0.6-beta.zip" "%~dp0..\Output\SOLUM-UI-Update.zip" >nul
copy /y "%~dp0..\Output\Solace-v0.0.6-beta.zip" "%~dp0..\Solace-v0.0.6-beta.zip" >nul

echo.
echo ========================================================
echo   SUCCESS! Files ready in Output folder:
echo   - SOLACE-UI-REPO\Output\SolaceSetup.exe
echo   - SOLACE-UI-REPO\Output\Solace-v0.0.6-beta.zip
echo ========================================================
echo.
pause

