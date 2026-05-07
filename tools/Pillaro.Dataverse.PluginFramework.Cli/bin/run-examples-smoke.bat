@echo off
setlocal EnableExtensions

rem ============================================================
rem Local smoke runner for Pillaro Dataverse CLI development build.
rem
rem Expected location:
rem   tools\Pillaro.Dataverse.PluginFramework.Cli\bin\run-examples-smoke.bat
rem
rem Expected CLI build:
rem   tools\Pillaro.Dataverse.PluginFramework.Cli\bin\Debug\net8.0\pillaro-dv.exe
rem
rem Usage:
rem   run-examples-smoke.bat manifest
rem   run-examples-smoke.bat validate
rem   run-examples-smoke.bat diff
rem   run-examples-smoke.bat deploy
rem   run-examples-smoke.bat all
rem
rem Required for diff/deploy:
rem   set "DV_CONN=AuthType=ClientSecret;Url=...;ClientId=...;ClientSecret=...;TenantId=..."
rem
rem Required for deploy when PAC push is enabled:
rem   set "DV_PAC=YourPacProfileName"
rem   set "DV_PLUGIN_ID=Existing Dataverse plugin assembly id"
rem
rem Optional:
rem   set "DV_SKIP_PAC_PUSH=1"
rem ============================================================

set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%..\..\..\"
set "CLI=%SCRIPT_DIR%Debug\net8.0\pillaro-dv.exe"
set "EXAMPLES_PLUGIN_PROJECT=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins\Pillaro.Dataverse.PluginFramework.Examples.Plugins.csproj"
set "EXAMPLES_PLUGIN_DLL_DEBUG=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins\bin\Debug\Pillaro.Dataverse.PluginFramework.Examples.Plugins.dll"
set "EXAMPLES_PLUGIN_DLL_RELEASE=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins\bin\Release\Pillaro.Dataverse.PluginFramework.Examples.Plugins.dll"
set "ARTIFACTS_DIR=%ROOT_DIR%artifacts\examples"
set "MANIFEST=%ARTIFACTS_DIR%\examples-plugin-manifest.json"

if "%~1"=="" (
    set "COMMAND=all"
) else (
    set "COMMAND=%~1"
)

if not exist "%CLI%" (
    echo CLI was not found:
    echo   %CLI%
    echo Build Pillaro.Dataverse.PluginFramework.Cli in Visual Studio first.
    exit /b 2
)

if exist "%EXAMPLES_PLUGIN_DLL_DEBUG%" (
    set "PLUGIN_DLL=%EXAMPLES_PLUGIN_DLL_DEBUG%"
) else if exist "%EXAMPLES_PLUGIN_DLL_RELEASE%" (
    set "PLUGIN_DLL=%EXAMPLES_PLUGIN_DLL_RELEASE%"
) else (
    echo Examples plugin assembly was not found.
    echo Expected one of:
    echo   %EXAMPLES_PLUGIN_DLL_DEBUG%
    echo   %EXAMPLES_PLUGIN_DLL_RELEASE%
    echo Build examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins in Visual Studio first.
    exit /b 3
)

if not exist "%ARTIFACTS_DIR%" mkdir "%ARTIFACTS_DIR%"

echo.
echo === Pillaro Dataverse Plugin Framework smoke runner ===
echo CLI:      %CLI%
echo Plugin:   %PLUGIN_DLL%
echo Manifest: %MANIFEST%
echo Command:  %COMMAND%
echo.

if /I "%COMMAND%"=="manifest" goto :manifest
if /I "%COMMAND%"=="validate" goto :validate
if /I "%COMMAND%"=="diff" goto :diff
if /I "%COMMAND%"=="deploy" goto :deploy
if /I "%COMMAND%"=="all" goto :all

echo Unknown command: %COMMAND%
echo Supported commands: manifest, validate, diff, deploy, all
exit /b 2

:all
call :manifest
if errorlevel 1 exit /b %errorlevel%
call :validate
if errorlevel 1 exit /b %errorlevel%
call :diff
exit /b %errorlevel%

:manifest
echo.
echo --- Generate manifest ---
"%CLI%" plugin manifest --assembly "%PLUGIN_DLL%" --output "%MANIFEST%"
exit /b %errorlevel%

:validate
echo.
echo --- Validate manifest ---
if not exist "%MANIFEST%" call :manifest
if errorlevel 1 exit /b %errorlevel%
"%CLI%" plugin validate --manifest "%MANIFEST%"
exit /b %errorlevel%

:diff
echo.
echo --- Diff manifest with Dataverse ---
if "%DV_CONN%"=="" (
    echo Missing DV_CONN environment variable.
    echo Example:
    echo   set "DV_CONN=AuthType=ClientSecret;Url=https://org.crm4.dynamics.com;ClientId=...;ClientSecret=...;TenantId=..."
    exit /b 10
)
if not exist "%MANIFEST%" call :manifest
if errorlevel 1 exit /b %errorlevel%
"%CLI%" plugin diff --manifest "%MANIFEST%" --conn "%DV_CONN%" --include-unchanged
exit /b %errorlevel%

:deploy
echo.
echo --- Deploy manifest to Dataverse ---
if "%DV_CONN%"=="" (
    echo Missing DV_CONN environment variable.
    exit /b 10
)
if not exist "%MANIFEST%" call :manifest
if errorlevel 1 exit /b %errorlevel%

set "PAC_ARGS="
if not "%DV_PAC%"=="" set "PAC_ARGS=--pac-profile "%DV_PAC%""

set "PUSH_ARGS="
if "%DV_SKIP_PAC_PUSH%"=="1" (
    set "PUSH_ARGS=--skip-pac-push"
) else (
    if "%DV_PLUGIN_ID%"=="" (
        echo Missing DV_PLUGIN_ID for PAC plugin push.
        echo Either set DV_PLUGIN_ID or set DV_SKIP_PAC_PUSH=1.
        exit /b 11
    )
    set "PUSH_ARGS=--plugin-id "%DV_PLUGIN_ID%" --plugin-type Assembly"
)

"%CLI%" plugin deploy --manifest "%MANIFEST%" --assembly "%PLUGIN_DLL%" --conn "%DV_CONN%" %PAC_ARGS% %PUSH_ARGS% --confirm --include-unchanged
exit /b %errorlevel%
