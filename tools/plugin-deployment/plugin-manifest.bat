@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"

if "%~1"=="" (
    echo Usage: plugin-manifest.bat ^<assembly-path^> [manifest-path]
    exit /b 10
)

set "PLUGIN_ASSEMBLY=%~1"
set "PLUGIN_MANIFEST=%~2"
if "%PLUGIN_MANIFEST%"=="" set "PLUGIN_MANIFEST=artifacts\plugin-manifest.json"

if not exist "%PLUGIN_ASSEMBLY%" (
    echo Plugin assembly was not found:
    echo   %PLUGIN_ASSEMBLY%
    exit /b 11
)

for %%I in ("%PLUGIN_MANIFEST%") do if not exist "%%~dpI" mkdir "%%~dpI"

"%SCRIPT_DIR%_run-pillaro-dv.bat" plugin manifest --assembly "%PLUGIN_ASSEMBLY%" --output "%PLUGIN_MANIFEST%"
exit /b %errorlevel%
