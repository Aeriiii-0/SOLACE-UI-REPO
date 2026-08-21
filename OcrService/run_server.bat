@echo off
title Solace OCR Service
cd /d "%~dp0"
echo ====================================================
echo          Starting Solace OCR Engine...
echo ====================================================
echo.
venv\Scripts\python.exe server.py
pause
