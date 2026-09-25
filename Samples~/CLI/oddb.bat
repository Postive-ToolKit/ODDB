@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0oddb.ps1" %*
exit /b %ERRORLEVEL%
