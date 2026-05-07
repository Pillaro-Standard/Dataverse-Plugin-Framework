@echo off
setlocal EnableExtensions

rem Internal helper. Usage:
rem   call _run-pillaro-dv.bat plugin <command> <args>

set "SCRIPT_DIR=%~dp0"
if "%DV_CLI%"=="" set "DV_CLI=pillaro-dv"

call "%SCRIPT_DIR%_load-profile.bat"
if errorlevel 1 exit /b %errorlevel%

set "PAC_ARGS=--pac-cli "%DV_PAC_CLI%""
if not "%DV_PAC%"=="" set "PAC_ARGS=%PAC_ARGS% --pac-profile "%DV_PAC%""

"%DV_CLI%" %* %PAC_ARGS% --conn "%DV_CONN%"
exit /b %errorlevel%
