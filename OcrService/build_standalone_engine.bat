@echo off
title Build Standalone Solace OCR Engine
cd /d "%~dp0"
echo ====================================================
echo      Building Standalone Solace OCR Engine (.exe)
echo ====================================================
echo.

where pyinstaller >nul 2>&1
if %errorlevel% neq 0 (
    echo [Step 1/2] Installing PyInstaller compiler...
    pip install pyinstaller
) else (
    echo [Step 1/2] PyInstaller compiler is ready.
)

echo.
echo [Step 2/2] Compiling self-contained ocr_server.exe...
pyinstaller --noconfirm --onedir --windowed --name "ocr_server" ^
  --add-data "models;models" ^
  --add-data "cswdo_template.json;." ^
  --hidden-import "uvicorn" ^
  --hidden-import "fastapi" ^
  --hidden-import "pydantic" ^
  --hidden-import "openvino" ^
  --hidden-import "paddleocr" ^
  server.py

echo.
echo ====================================================
echo  Build Complete!
echo  Standalone engine folder created at: dist\ocr_server
echo ====================================================
pause
