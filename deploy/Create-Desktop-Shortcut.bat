@echo off
setlocal
echo Creating Desktop Shortcut for Solace...

set "TARGET_DIR=%~dp0"
if "%TARGET_DIR:~-1%"=="\" set "TARGET_DIR=%TARGET_DIR:~0,-1%"

:: If run from deploy folder, target is ..\bin\Release\SOLUM-UI.exe, otherwise check current folder
if exist "%TARGET_DIR%\SOLUM-UI.exe" (
    set "EXE_PATH=%TARGET_DIR%\SOLUM-UI.exe"
    set "WORK_DIR=%TARGET_DIR%"
) else if exist "%TARGET_DIR%\..\bin\Release\SOLUM-UI.exe" (
    set "EXE_PATH=%TARGET_DIR%\..\bin\Release\SOLUM-UI.exe"
    set "WORK_DIR=%TARGET_DIR%\..\bin\Release"
) else (
    echo Error: Could not locate SOLUM-UI.exe.
    pause
    exit /b 1
)

powershell -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([System.IO.Path]::Combine([System.Environment]::GetFolderPath('Desktop'), 'Solace.lnk')); $s.TargetPath = '%EXE_PATH%'; $s.WorkingDirectory = '%WORK_DIR%'; $s.Description = 'Solace - Solo Parent Registry System'; $s.Save()"

echo Success! Desktop shortcut created.
pause
