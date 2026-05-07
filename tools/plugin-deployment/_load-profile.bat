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
if "%PILLARO_DV_SDK_AUTH_TYPE%"=="" set "PILLARO_DV_SDK_AUTH_TYPE=ClientSecret"

if /I "%PILLARO_DV_SDK_AUTH_TYPE%"=="ConnectionString" goto :validate_sdk_connection_string
if /I "%PILLARO_DV_SDK_AUTH_TYPE%"=="ClientSecret" goto :validate_sdk_environment_url
if /I "%PILLARO_DV_SDK_AUTH_TYPE%"=="Interactive" goto :validate_sdk_environment_url

echo Unsupported PILLARO_DV_SDK_AUTH_TYPE: %PILLARO_DV_SDK_AUTH_TYPE%
echo Supported values: ClientSecret, ConnectionString, Interactive.
exit /b 22

:validate_sdk_environment_url
if "%PILLARO_DV_SDK_ENVIRONMENT%"=="" (
    echo Missing PILLARO_DV_SDK_ENVIRONMENT.
    echo Set it in local profile or as a secure pipeline variable.
    exit /b 21
)

if /I "%PILLARO_DV_SDK_AUTH_TYPE%"=="Interactive" goto :success
goto :validate_sdk_client_secret

:validate_sdk_connection_string
if "%PILLARO_DV_SDK_CONNECTION_STRING%"=="" (
    echo Missing PILLARO_DV_SDK_CONNECTION_STRING for ConnectionString authentication.
    exit /b 23
)
goto :success

:validate_sdk_client_secret
if "%PILLARO_DV_SDK_TENANT_ID%"=="" (
    echo Missing PILLARO_DV_SDK_TENANT_ID for ClientSecret authentication.
    exit /b 24
)
if "%PILLARO_DV_SDK_CLIENT_ID%"=="" (
    echo Missing PILLARO_DV_SDK_CLIENT_ID for ClientSecret authentication.
    exit /b 25
)
if "%PILLARO_DV_SDK_CLIENT_SECRET%"=="" (
    echo Missing PILLARO_DV_SDK_CLIENT_SECRET for ClientSecret authentication.
    exit /b 26
)
goto :success

:success
endlocal & (
    set "PILLARO_DV_PROFILE=%PILLARO_DV_PROFILE%"
    set "PILLARO_DV_PROFILE_FILE=%PILLARO_DV_PROFILE_FILE%"
    set "PILLARO_DV_PAC_AUTH_PROFILE=%PILLARO_DV_PAC_AUTH_PROFILE%"
    set "PILLARO_DV_PAC_CLI=%PILLARO_DV_PAC_CLI%"
    set "PILLARO_DV_SDK_AUTH_TYPE=%PILLARO_DV_SDK_AUTH_TYPE%"
    set "PILLARO_DV_SDK_ENVIRONMENT=%PILLARO_DV_SDK_ENVIRONMENT%"
    set "PILLARO_DV_SDK_TENANT_ID=%PILLARO_DV_SDK_TENANT_ID%"
    set "PILLARO_DV_SDK_CLIENT_ID=%PILLARO_DV_SDK_CLIENT_ID%"
    set "PILLARO_DV_SDK_CLIENT_SECRET=%PILLARO_DV_SDK_CLIENT_SECRET%"
    set "PILLARO_DV_SDK_CONNECTION_STRING=%PILLARO_DV_SDK_CONNECTION_STRING%"
)
exit /b 0
