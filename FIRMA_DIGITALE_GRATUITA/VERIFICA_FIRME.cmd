@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp002_VERIFICA_FIRME.ps1"
if errorlevel 1 pause
