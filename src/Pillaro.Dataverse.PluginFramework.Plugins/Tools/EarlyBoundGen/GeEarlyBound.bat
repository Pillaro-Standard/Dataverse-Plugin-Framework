@echo off
setlocal

set SETTINGS=%~dp0..\..\EarlyBoundSettings.json
set OUTPUT=%~dp0..\..\Generated\EarlyBound

pac modelbuilder build --outdirectory "%OUTPUT%" --settingsTemplateFile "%SETTINGS%"

set EXITCODE=%ERRORLEVEL%
endlocal & exit /b %EXITCODE%