@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"

if "%~1"=="" (
    echo Usage: plugin-deploy.bat ^<assembly-path^> [manifest-path] [solution-name]
    exit /b 10
)

set "PLUGIN_ASSEMBLY=%~1"
set "PLUGIN_MANIFEST=%~2"
set "PLUGIN_SOLUTION=%~3"

if "%PLUGIN_MANIFEST%"=="" set "PLUGIN_MANIFEST=artifacts\plugin-manifest.json"
if "%PLUGIN_SOLUTION%"=="" set "PLUGIN_SOLUTION=%PILLARO_DV_SOLUTION%"

if not exist "%PLUGIN_ASSEMBLY%" (
    echo Plugin assembly was not found:
    echo   %PLUGIN_ASSEMBLY%
    exit /b 11
)

if not exist "%PLUGIN_MANIFEST%" (
    echo Plugin manifest was not found:
    echo   %PLUGIN_MANIFEST%
    echo.
    echo Run plugin-manifest.bat first or pass an existing manifest path.
    exit /b 12
)

set "SOLUTION_ARGS="
if not "%PLUGIN_SOLUTION%"=="" set "SOLUTION_ARGS=--solution "%PLUGIN_SOLUTION%""

"%SCRIPT_DIR%_run-pillaro-dv.bat" plugin deploy --assembly "%PLUGIN_ASSEMBLY%" --manifest "%PLUGIN_MANIFEST%" %SOLUTION_ARGS%
exit /b %errorlevel%
