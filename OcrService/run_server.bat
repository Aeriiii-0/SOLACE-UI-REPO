@echo off
title Solace OCR Service
cd /d "%~dp0"
echo ====================================================
echo          Starting Solace OCR Engine...
echo ====================================================
echo Freeing port 8000 if occupied...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :8000 ^| findstr LISTENING') do taskkill /f /pid %%a 2>nul
echo.
venv\Scripts\python.exe server.py
pause
