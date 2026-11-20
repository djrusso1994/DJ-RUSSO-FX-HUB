@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp001_CREA_CERTIFICATO_E_FIRMA_RELEASE.ps1"
if errorlevel 1 pause
