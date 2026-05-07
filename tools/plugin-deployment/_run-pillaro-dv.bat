@echo off
setlocal EnableExtensions

rem Internal helper. Usage:
rem   call _run-pillaro-dv.bat plugin <command> <args>

set "SCRIPT_DIR=%~dp0"
if "%PILLARO_DV_CLI%"=="" set "PILLARO_DV_CLI=pillaro-dv"

call "%SCRIPT_DIR%_load-profile.bat"
if errorlevel 1 exit /b %errorlevel%

set "PILLARO_DV_AUTH_ARGS=--environment "%PILLARO_DV_URL%" --auth-type "%PILLARO_DV_AUTH_TYPE%""

if /I "%PILLARO_DV_AUTH_TYPE%"=="ClientSecret" (
    set "PILLARO_DV_AUTH_ARGS=%PILLARO_DV_AUTH_ARGS% --tenant-id "%PILLARO_DV_TENANT_ID%" --client-id "%PILLARO_DV_CLIENT_ID%" --client-secret "%PILLARO_DV_CLIENT_SECRET%""
)

if /I "%PILLARO_DV_AUTH_TYPE%"=="ConnectionString" (
    set "PILLARO_DV_AUTH_ARGS=--connection-string "%PILLARO_DV_CONNECTION_STRING%""
)

%PILLARO_DV_CLI% %* %PILLARO_DV_AUTH_ARGS%
exit /b %errorlevel%
