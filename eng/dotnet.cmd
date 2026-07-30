@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0dotnet.ps1" %*
exit /b %ERRORLEVEL%
