@echo off
setlocal EnableExtensions

rem ============================================================
rem Local smoke runner for Pillaro Dataverse CLI development build.
rem
rem Project default profile:
rem   .dv-profile
rem
rem Local secret profile:
rem   %USERPROFILE%\.dv\profiles\<profile>.cmd
rem
rem Override profile:
rem   set "DV_PROFILE=other-profile"
rem
rem List profiles:
rem   run-examples-smoke.bat profiles
rem ============================================================

set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%..\..\..\"
set "CLI=%SCRIPT_DIR%Debug\net8.0\pillaro-dv.exe"
set "MANIFEST_SOURCE_DLL_DEBUG=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Logic\bin\Debug\Pillaro.Dataverse.PluginFramework.Examples.Logic.dll"
set "MANIFEST_SOURCE_DLL_RELEASE=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Logic\bin\Release\Pillaro.Dataverse.PluginFramework.Examples.Logic.dll"
set "DEPLOY_PLUGIN_DLL_DEBUG=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins\bin\Debug\Pillaro.Dataverse.PluginFramework.Examples.Plugins.dll"
set "DEPLOY_PLUGIN_DLL_RELEASE=%ROOT_DIR%examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins\bin\Release\Pillaro.Dataverse.PluginFramework.Examples.Plugins.dll"
set "ARTIFACTS_DIR=%ROOT_DIR%artifacts\examples"
set "MANIFEST=%ARTIFACTS_DIR%\examples-plugin-manifest.json"
set "PROFILE_ROOT=%USERPROFILE%\.dv\profiles"

if "%~1"=="" (
    set "COMMAND=all"
) else (
    set "COMMAND=%~1"
)

if /I "%COMMAND%"=="profiles" goto :profiles

call :loadProfile
if errorlevel 1 exit /b %errorlevel%

if not exist "%CLI%" (
    echo CLI was not found:
    echo   %CLI%
    echo Build Pillaro.Dataverse.PluginFramework.Cli in Visual Studio first.
    exit /b 2
)

if exist "%MANIFEST_SOURCE_DLL_DEBUG%" (
    set "MANIFEST_SOURCE_DLL=%MANIFEST_SOURCE_DLL_DEBUG%"
) else if exist "%MANIFEST_SOURCE_DLL_RELEASE%" (
    set "MANIFEST_SOURCE_DLL=%MANIFEST_SOURCE_DLL_RELEASE%"
) else (
    echo Manifest source assembly was not found.
    echo Expected one of:
    echo   %MANIFEST_SOURCE_DLL_DEBUG%
    echo   %MANIFEST_SOURCE_DLL_RELEASE%
    echo Build examples\Pillaro.Dataverse.PluginFramework.Examples.Logic in Visual Studio first.
    exit /b 3
)

if exist "%DEPLOY_PLUGIN_DLL_DEBUG%" (
    set "DEPLOY_PLUGIN_DLL=%DEPLOY_PLUGIN_DLL_DEBUG%"
) else if exist "%DEPLOY_PLUGIN_DLL_RELEASE%" (
    set "DEPLOY_PLUGIN_DLL=%DEPLOY_PLUGIN_DLL_RELEASE%"
) else (
    echo Deploy plugin assembly was not found.
    echo Expected one of:
    echo   %DEPLOY_PLUGIN_DLL_DEBUG%
    echo   %DEPLOY_PLUGIN_DLL_RELEASE%
    echo Build examples\Pillaro.Dataverse.PluginFramework.Examples.Plugins in Visual Studio first.
    exit /b 4
)

if not exist "%ARTIFACTS_DIR%" mkdir "%ARTIFACTS_DIR%"

echo.
echo === Pillaro Dataverse Plugin Framework smoke runner ===
echo Profile:         %DV_PROFILE%
echo CLI:             %CLI%
echo Manifest source: %MANIFEST_SOURCE_DLL%
echo Deploy plugin:   %DEPLOY_PLUGIN_DLL%
echo Manifest:        %MANIFEST%
echo Command:         %COMMAND%
echo.

if /I "%COMMAND%"=="manifest" goto :manifest
if /I "%COMMAND%"=="validate" goto :validate
if /I "%COMMAND%"=="diff" goto :diff
if /I "%COMMAND%"=="deploy" goto :deploy
if /I "%COMMAND%"=="all" goto :all

echo Unknown command: %COMMAND%
echo Supported commands: manifest, validate, diff, deploy, all, profiles
exit /b 2

:loadProfile
if "%DV_PROFILE%"=="" (
    if exist "%ROOT_DIR%.dv-profile" (
        set /p DV_PROFILE=<"%ROOT_DIR%.dv-profile"
    )
)
if "%DV_PROFILE%"=="" set "DV_PROFILE=default"

set "PROFILE_FILE=%PROFILE_ROOT%\%DV_PROFILE%.cmd"
if exist "%PROFILE_FILE%" (
    call "%PROFILE_FILE%"
    exit /b 0
)

if "%DV_CONN%"=="" (
    echo Dataverse profile was not found and DV_CONN is not set.
    echo.
    echo Selected profile:
    echo   %DV_PROFILE%
    echo.
    echo Expected profile file:
    echo   %PROFILE_FILE%
    echo.
    echo Create it from:
    echo   tools\Pillaro.Dataverse.PluginFramework.Cli\bin\local-profile.template.cmd
    echo.
    echo Available profiles:
    call :profiles
    exit /b 10
)

exit /b 0

:profiles
echo.
echo Dataverse profiles in %PROFILE_ROOT%:
if not exist "%PROFILE_ROOT%" (
    echo   ^<profile folder does not exist^>
    echo.
    echo Create folder:
    echo   mkdir "%PROFILE_ROOT%"
    exit /b 0
)
for %%F in ("%PROFILE_ROOT%\*.cmd") do echo   %%~nF
exit /b 0

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
"%CLI%" plugin manifest --assembly "%MANIFEST_SOURCE_DLL%" --output "%MANIFEST%"
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
    echo Missing DV_CONN. Profile '%DV_PROFILE%' did not set it.
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
    echo Missing DV_CONN. Profile '%DV_PROFILE%' did not set it.
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
        echo Missing DV_PLUGIN_ID for PAC plugin push in profile '%DV_PROFILE%'.
        echo Either set DV_PLUGIN_ID or set DV_SKIP_PAC_PUSH=1.
        exit /b 11
    )
    set "PUSH_ARGS=--plugin-id "%DV_PLUGIN_ID%" --plugin-type Assembly"
)

"%CLI%" plugin deploy --manifest "%MANIFEST%" --assembly "%DEPLOY_PLUGIN_DLL%" --conn "%DV_CONN%" %PAC_ARGS% %PUSH_ARGS% --confirm --include-unchanged
exit /b %errorlevel%
