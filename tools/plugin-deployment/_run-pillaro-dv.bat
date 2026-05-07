@echo off
setlocal EnableExtensions

rem Internal helper. Usage:
rem   call _run-pillaro-dv.bat plugin <command> <args>

set "SCRIPT_DIR=%~dp0"
if "%PILLARO_DV_CLI%"=="" set "PILLARO_DV_CLI=pillaro-dv"

call "%SCRIPT_DIR%_load-profile.bat"
if errorlevel 1 exit /b %errorlevel%

set "PAC_ARGS=--pac-cli "%PILLARO_DV_PAC_CLI%""
if not "%PILLARO_DV_PAC_AUTH_PROFILE%"=="" set "PAC_ARGS=%PAC_ARGS% --pac-auth-profile "%PILLARO_DV_PAC_AUTH_PROFILE%""

"%PILLARO_DV_CLI%" %* %PAC_ARGS% --sdk-connection-string "%PILLARO_DV_SDK_CONNECTION_STRING%"
exit /b %errorlevel%
