@echo off
title Build Standalone Solace OCR Engine
cd /d "%~dp0"
echo ====================================================
echo      Building Standalone Solace OCR Engine (.exe)
echo ====================================================
echo.

where pyinstaller >nul 2>&1
if %errorlevel% neq 0 (
    echo [Step 1/3] Installing PyInstaller compiler...
    pip install pyinstaller
) else (
    echo [Step 1/3] PyInstaller compiler is ready.
)

echo.
echo [Step 2/3] Compiling self-contained ocr_server.exe...
pyinstaller --noconfirm --onedir --name "ocr_server" ^
  --add-data "models;models" ^
  --add-data "cswdo_template.json;." ^
  --copy-metadata "paddleocr" ^
  --copy-metadata "paddlex" ^
  --copy-metadata "opencv-contrib-python" ^
  --copy-metadata "chardet" ^
  --collect-data "paddlex" ^
  --collect-data "paddleocr" ^
  --collect-all "chardet" ^
  --hidden-import "uvicorn" ^
  --hidden-import "fastapi" ^
  --hidden-import "pydantic" ^
  --hidden-import "openvino" ^
  --hidden-import "paddleocr" ^
  server.py

echo.
echo [Step 3/3] Copying runtime dependencies and models...
copy /y "cswdo_template.json" "dist\ocr_server\cswdo_template.json" >nul
copy /y "cswdo_template.json" "dist\ocr_server\_internal\cswdo_template.json" >nul
xcopy /e /i /y "models" "dist\ocr_server\models" >nul
xcopy /e /i /y "models" "dist\ocr_server\_internal\models" >nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$sp = 'C:\Users\Renzo\AppData\Local\Programs\Python\Python311\Lib\site-packages'; $dest = 'dist\ocr_server\_internal'; Get-ChildItem \"$sp\*.dist-info\" | Where-Object { $_.Name -like '*opencv*' -or $_.Name -like '*paddle*' -or $_.Name -like '*chardet*' -or $_.Name -like '*paddlex*' } | ForEach-Object { Copy-Item -Path $_.FullName -Destination \"$dest\$($_.Name)\" -Recurse -Force }; Copy-Item -Path \"$sp\paddle\libs\*\" -Destination \"$dest\" -Force; Copy-Item -Path \"$sp\paddle\libs\*\" -Destination 'dist\ocr_server\' -Force; New-Item -ItemType Directory -Path \"$dest\paddle\libs\" -Force | Out-Null; Copy-Item -Path \"$sp\paddle\libs\*\" -Destination \"$dest\paddle\libs\" -Force; Copy-Item -Path \"$sp\chardet\" -Destination \"$dest\chardet\" -Recurse -Force; Copy-Item -Path \"$sp\chardet\models\_format__mypyc.cp311-win_amd64.pyd\" -Destination \"$dest\" -Force"

echo.
echo ====================================================
echo  Build Complete!
echo  Standalone engine folder created at: dist\ocr_server
echo ====================================================
pause
