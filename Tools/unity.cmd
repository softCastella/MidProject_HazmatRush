@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0unity.ps1" %*
exit /b %ERRORLEVEL%
