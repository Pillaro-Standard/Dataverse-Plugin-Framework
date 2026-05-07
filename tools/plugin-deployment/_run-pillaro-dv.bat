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

if /I "%PILLARO_DV_SDK_AUTH_TYPE%"=="ConnectionString" (
    "%PILLARO_DV_CLI%" %* %PAC_ARGS% --sdk-auth-type ConnectionString --sdk-connection-string "%PILLARO_DV_SDK_CONNECTION_STRING%"
    exit /b %errorlevel%
)

if /I "%PILLARO_DV_SDK_AUTH_TYPE%"=="ClientSecret" (
    "%PILLARO_DV_CLI%" %* %PAC_ARGS% --sdk-auth-type ClientSecret --sdk-environment "%PILLARO_DV_SDK_ENVIRONMENT%" --sdk-tenant-id "%PILLARO_DV_SDK_TENANT_ID%" --sdk-client-id "%PILLARO_DV_SDK_CLIENT_ID%" --sdk-client-secret "%PILLARO_DV_SDK_CLIENT_SECRET%"
    exit /b %errorlevel%
)

"%PILLARO_DV_CLI%" %* %PAC_ARGS% --sdk-auth-type "%PILLARO_DV_SDK_AUTH_TYPE%" --sdk-environment "%PILLARO_DV_SDK_ENVIRONMENT%"
exit /b %errorlevel%
