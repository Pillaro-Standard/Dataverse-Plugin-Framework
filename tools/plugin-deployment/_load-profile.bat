@echo off
setlocal EnableExtensions

rem Internal helper loaded by deployment scripts.
rem It supports two credential modes:
rem 1) Local developer profile stored outside repository under %USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\<profile>.cmd
rem 2) Pipeline secure variables already available as environment variables.

if "%PILLARO_DV_PROFILE%"=="" set "PILLARO_DV_PROFILE=default"

if /I "%PILLARO_DV_AUTH_MODE%"=="pipeline" goto :validate

set "PILLARO_DV_PROFILE_FILE=%USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\%PILLARO_DV_PROFILE%.cmd"

if exist "%PILLARO_DV_PROFILE_FILE%" (
    call "%PILLARO_DV_PROFILE_FILE%"
) else (
    if /I "%PILLARO_DV_AUTH_MODE%"=="local" (
        echo Local Dataverse profile was not found:
        echo   %PILLARO_DV_PROFILE_FILE%
        echo.
        echo Create it from tools\plugin-deployment\local-profile.template.cmd.
        exit /b 20
    )
)

:validate
if "%PILLARO_DV_PAC_CLI%"=="" set "PILLARO_DV_PAC_CLI=pac"

if "%PILLARO_DV_SDK_CONNECTION_STRING%"=="" (
    echo Missing PILLARO_DV_SDK_CONNECTION_STRING.
    echo Put the complete Dataverse SDK connection string into the local profile or secure pipeline variable.
    exit /b 23
)

:success
endlocal & (
    set "PILLARO_DV_PROFILE=%PILLARO_DV_PROFILE%"
    set "PILLARO_DV_PROFILE_FILE=%PILLARO_DV_PROFILE_FILE%"
    set "PILLARO_DV_PAC_AUTH_PROFILE=%PILLARO_DV_PAC_AUTH_PROFILE%"
    set "PILLARO_DV_PAC_CLI=%PILLARO_DV_PAC_CLI%"
    set "PILLARO_DV_SDK_CONNECTION_STRING=%PILLARO_DV_SDK_CONNECTION_STRING%"
)
exit /b 0
