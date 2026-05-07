@echo off
setlocal EnableExtensions

rem Internal helper. Usage:
rem   call _run-pillaro-dv.bat plugin <command> <args>

set "SCRIPT_DIR=%~dp0"
if "%PILLARO_DV_CLI%"=="" set "PILLARO_DV_CLI=pillaro-dv"

call "%SCRIPT_DIR%_load-profile.bat"
if errorlevel 1 exit /b %errorlevel%

if /I "%PILLARO_DV_AUTH_TYPE%"=="PacCli" (
    if "%PILLARO_DV_PAC_AUTH_PROFILE%"=="" (
        "%PILLARO_DV_CLI%" %* --auth-type PacCli --pac-cli "%PILLARO_DV_PAC_CLI%"
    ) else (
        "%PILLARO_DV_CLI%" %* --auth-type PacCli --pac-cli "%PILLARO_DV_PAC_CLI%" --pac-auth-profile "%PILLARO_DV_PAC_AUTH_PROFILE%"
    )
    exit /b %errorlevel%
)

if /I "%PILLARO_DV_AUTH_TYPE%"=="ConnectionString" (
    "%PILLARO_DV_CLI%" %* --auth-type ConnectionString --connection-string "%PILLARO_DV_CONNECTION_STRING%"
    exit /b %errorlevel%
)

if /I "%PILLARO_DV_AUTH_TYPE%"=="ClientSecret" (
    "%PILLARO_DV_CLI%" %* --environment "%PILLARO_DV_URL%" --auth-type "%PILLARO_DV_AUTH_TYPE%" --tenant-id "%PILLARO_DV_TENANT_ID%" --client-id "%PILLARO_DV_CLIENT_ID%" --client-secret "%PILLARO_DV_CLIENT_SECRET%"
    exit /b %errorlevel%
)

"%PILLARO_DV_CLI%" %* --environment "%PILLARO_DV_URL%" --auth-type "%PILLARO_DV_AUTH_TYPE%"
exit /b %errorlevel%
