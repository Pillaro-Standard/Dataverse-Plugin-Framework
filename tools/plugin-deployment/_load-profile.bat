@echo off
setlocal EnableExtensions

rem Internal helper loaded by deployment scripts.
rem Local profile path: %USERPROFILE%\.dv\profiles\<profile>.cmd

if "%DV_PROFILE%"=="" set "DV_PROFILE=default"

if /I "%DV_MODE%"=="pipeline" goto :validate

set "DV_PROFILE_FILE=%USERPROFILE%\.dv\profiles\%DV_PROFILE%.cmd"

if exist "%DV_PROFILE_FILE%" (
    call "%DV_PROFILE_FILE%"
) else (
    if /I "%DV_MODE%"=="local" (
        echo Local Dataverse profile was not found:
        echo   %DV_PROFILE_FILE%
        echo.
        echo Create it from tools\plugin-deployment\local-profile.template.cmd.
        exit /b 20
    )
)

:validate
if "%DV_PAC_CLI%"=="" set "DV_PAC_CLI=pac"

if "%DV_CONN%"=="" (
    echo Missing DV_CONN.
    echo Put the complete Dataverse connection string into the local profile or secure pipeline variable.
    exit /b 23
)

:success
endlocal & (
    set "DV_PROFILE=%DV_PROFILE%"
    set "DV_PROFILE_FILE=%DV_PROFILE_FILE%"
    set "DV_PAC=%DV_PAC%"
    set "DV_PAC_CLI=%DV_PAC_CLI%"
    set "DV_CONN=%DV_CONN%"
)
exit /b 0
