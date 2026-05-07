@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"

set "PLUGIN_MANIFEST=%~1"
if "%PLUGIN_MANIFEST%"=="" set "PLUGIN_MANIFEST=artifacts\plugin-manifest.json"

if not exist "%PLUGIN_MANIFEST%" (
    echo Plugin manifest was not found:
    echo   %PLUGIN_MANIFEST%
    exit /b 11
)

"%SCRIPT_DIR%_run-pillaro-dv.bat" plugin diff --manifest "%PLUGIN_MANIFEST%"
exit /b %errorlevel%
