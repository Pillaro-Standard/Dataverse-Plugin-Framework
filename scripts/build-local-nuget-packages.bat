@echo off
setlocal EnableExtensions

REM ============================================================
REM Build local NuGet packages for Pillaro Dataverse Plugin Framework
REM ============================================================
REM
REM Location:
REM - place this BAT file in the <repository>\scripts directory
REM
REM Usage:
REM - run it from any working directory or double-click it
REM - enter package version when prompted
REM
REM NuGet CLI:
REM - first tries to find nuget.exe in PATH
REM - then checks the user-local Pillaro tools directory
REM - if missing, downloads it automatically
REM
REM Output:
REM - C:\LocalNuGet\Pillaro.Dataverse.PluginFramework.<version>.nupkg
REM - C:\LocalNuGet\Pillaro.Dataverse.PluginFramework.Testing.<version>.nupkg
REM ============================================================


REM ============================================================
REM General configuration
REM ============================================================

REM The BAT file is located in <repository>\scripts.
REM Resolve the repository root as the parent directory.
for %%I in ("%~dp0..") do set "ROOT=%%~fI"

set "CONFIGURATION=Debug"
set "TARGET_FRAMEWORK=net8.0"
set "OUTPUT_DIR=C:\LocalNuGet"
set "DEFAULT_VERSION=0.0.6-local"


REM ============================================================
REM Project paths
REM ============================================================

set "CLI_PROJECT=%ROOT%\tools\Pillaro.Dataverse.PluginFramework.Cli\Pillaro.Dataverse.PluginFramework.Cli.csproj"

set "FRAMEWORK_PROJECT=%ROOT%\src\Pillaro.Dataverse.PluginFramework\Pillaro.Dataverse.PluginFramework.csproj"
set "FRAMEWORK_NUSPEC=%ROOT%\src\Pillaro.Dataverse.PluginFramework\Tools\PluginPackaging\Pillaro.Dataverse.PluginFramework.nuspec"
set "FRAMEWORK_BASEPATH=%ROOT%\src\Pillaro.Dataverse.PluginFramework"

set "TESTING_PROJECT=%ROOT%\src\Pillaro.Dataverse.PluginFramework.Testing\Pillaro.Dataverse.PluginFramework.Testing.csproj"
set "TESTING_NUSPEC=%ROOT%\src\Pillaro.Dataverse.PluginFramework.Testing\Tools\TestingPackaging\Pillaro.Dataverse.PluginFramework.Testing.nuspec"
set "TESTING_BASEPATH=%ROOT%\src\Pillaro.Dataverse.PluginFramework.Testing"


REM ============================================================
REM Resolve NuGet CLI
REM ============================================================

set "NUGET_DIR=%LOCALAPPDATA%\Pillaro\Tools\NuGet"
set "NUGET_LOCAL=%NUGET_DIR%\nuget.exe"
set "NUGET_TEMP=%NUGET_DIR%\nuget.exe.download"
set "NUGET_URL=https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

set "NUGET_EXE="

REM First try to find NuGet in PATH.
for /f "delims=" %%I in ('where nuget.exe 2^>nul') do (
    if not defined NUGET_EXE set "NUGET_EXE=%%~fI"
)

REM Verify the NuGet executable found in PATH.
if defined NUGET_EXE (
    "%NUGET_EXE%" help >nul 2>&1

    if errorlevel 1 (
        echo WARNING: NuGet CLI found in PATH could not be started:
        echo %NUGET_EXE%
        echo.
        set "NUGET_EXE="
    )
)

REM If NuGet was not found in PATH, try the user-local copy.
if not defined NUGET_EXE (
    if exist "%NUGET_LOCAL%" (
        "%NUGET_LOCAL%" help >nul 2>&1

        if not errorlevel 1 (
            set "NUGET_EXE=%NUGET_LOCAL%"
        )
    )
)

REM Delete an invalid local NuGet executable.
if not defined NUGET_EXE (
    if exist "%NUGET_LOCAL%" (
        echo WARNING: Existing local NuGet CLI is invalid and will be replaced:
        echo %NUGET_LOCAL%
        echo.

        del /q "%NUGET_LOCAL%" 2>nul
    )
)

REM Download NuGet if it is still unavailable.
if not defined NUGET_EXE (
    echo.
    echo NuGet CLI was not found.
    echo It will be downloaded automatically to:
    echo %NUGET_LOCAL%
    echo.

    if not exist "%NUGET_DIR%" (
        mkdir "%NUGET_DIR%" 2>nul
    )

    if not exist "%NUGET_DIR%" (
        echo ERROR: NuGet directory could not be created:
        echo %NUGET_DIR%
        exit /b 1
    )

    del /q "%NUGET_TEMP%" 2>nul

    REM Prefer curl.exe when available.
    where curl.exe >nul 2>&1

    if not errorlevel 1 (
        echo Downloading NuGet CLI using curl...

        curl.exe ^
          --fail ^
          --location ^
          --silent ^
          --show-error ^
          "%NUGET_URL%" ^
          --output "%NUGET_TEMP%"

        if errorlevel 1 (
            del /q "%NUGET_TEMP%" 2>nul
        )
    )

    REM Fall back to Windows PowerShell if curl is unavailable or failed.
    if not exist "%NUGET_TEMP%" (
        echo Downloading NuGet CLI using PowerShell...

        powershell.exe ^
          -NoLogo ^
          -NoProfile ^
          -ExecutionPolicy Bypass ^
          -Command ^
          "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest -UseBasicParsing -Uri '%NUGET_URL%' -OutFile '%NUGET_TEMP%'"

        if errorlevel 1 (
            del /q "%NUGET_TEMP%" 2>nul
        )
    )

    if not exist "%NUGET_TEMP%" (
        echo.
        echo ERROR: NuGet CLI could not be downloaded.
        echo Check your internet connection, firewall or proxy configuration.
        echo.
        echo Download URL:
        echo %NUGET_URL%
        exit /b 1
    )

    move /y "%NUGET_TEMP%" "%NUGET_LOCAL%" >nul

    if errorlevel 1 (
        echo.
        echo ERROR: Downloaded NuGet CLI could not be saved:
        echo %NUGET_LOCAL%
        exit /b 1
    )

    set "NUGET_EXE=%NUGET_LOCAL%"
)

REM Final NuGet executable verification.
"%NUGET_EXE%" help >nul 2>&1

if errorlevel 1 (
    echo.
    echo ERROR: NuGet CLI could not be started:
    echo %NUGET_EXE%
    exit /b 1
)


REM ============================================================
REM Package version
REM ============================================================

echo.
echo Pillaro Dataverse Plugin Framework local package build
echo ------------------------------------------------------------
echo Repository root: %ROOT%
echo Default version: %DEFAULT_VERSION%
echo.

REM Clear any inherited environment variable before prompting.
set "PACKAGE_VERSION="
set /p "PACKAGE_VERSION=Enter package version, or press ENTER to use default: "

if not defined PACKAGE_VERSION (
    set "PACKAGE_VERSION=%DEFAULT_VERSION%"
)


REM ============================================================
REM Configuration summary
REM ============================================================

echo.
echo Version:          %PACKAGE_VERSION%
echo Configuration:    %CONFIGURATION%
echo Target framework: %TARGET_FRAMEWORK%
echo Repository root:  %ROOT%
echo NuGet executable: %NUGET_EXE%
echo Output:           %OUTPUT_DIR%
echo.


REM ============================================================
REM Prerequisite validation
REM ============================================================

where dotnet.exe >nul 2>&1

if errorlevel 1 (
    echo ERROR: dotnet.exe was not found in PATH.
    echo Install the required .NET SDK and reopen the command prompt.
    exit /b 1
)

if not exist "%CLI_PROJECT%" (
    echo ERROR: CLI project was not found:
    echo %CLI_PROJECT%
    exit /b 1
)

if not exist "%FRAMEWORK_PROJECT%" (
    echo ERROR: Framework project was not found:
    echo %FRAMEWORK_PROJECT%
    exit /b 1
)

if not exist "%FRAMEWORK_NUSPEC%" (
    echo ERROR: Framework nuspec was not found:
    echo %FRAMEWORK_NUSPEC%
    exit /b 1
)

if not exist "%FRAMEWORK_BASEPATH%" (
    echo ERROR: Framework base path was not found:
    echo %FRAMEWORK_BASEPATH%
    exit /b 1
)

if not exist "%TESTING_PROJECT%" (
    echo ERROR: Testing project was not found:
    echo %TESTING_PROJECT%
    exit /b 1
)

if not exist "%TESTING_NUSPEC%" (
    echo ERROR: Testing nuspec was not found:
    echo %TESTING_NUSPEC%
    exit /b 1
)

if not exist "%TESTING_BASEPATH%" (
    echo ERROR: Testing base path was not found:
    echo %TESTING_BASEPATH%
    exit /b 1
)

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%" 2>nul
)

if not exist "%OUTPUT_DIR%" (
    echo ERROR: Output directory could not be created:
    echo %OUTPUT_DIR%
    exit /b 1
)


REM ============================================================
REM Publish CLI
REM ============================================================

echo.
echo ============================================================
echo Publishing CLI
echo ============================================================

dotnet publish "%CLI_PROJECT%" ^
  -c "%CONFIGURATION%" ^
  -f "%TARGET_FRAMEWORK%" ^
  --no-self-contained ^
  --nologo

if errorlevel 1 (
    echo.
    echo ERROR: CLI publishing failed.
    exit /b 1
)


REM ============================================================
REM Build framework
REM ============================================================

echo.
echo ============================================================
echo Building framework project
echo ============================================================

dotnet build "%FRAMEWORK_PROJECT%" ^
  -c "%CONFIGURATION%" ^
  --nologo

if errorlevel 1 (
    echo.
    echo ERROR: Framework project build failed.
    exit /b 1
)


REM ============================================================
REM Build testing framework
REM ============================================================

echo.
echo ============================================================
echo Building testing project
echo ============================================================

dotnet build "%TESTING_PROJECT%" ^
  -c "%CONFIGURATION%" ^
  --nologo

if errorlevel 1 (
    echo.
    echo ERROR: Testing project build failed.
    exit /b 1
)


REM ============================================================
REM Clean previous packages
REM ============================================================

echo.
echo ============================================================
echo Cleaning previous local packages with the same version
echo ============================================================

del /q "%OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.%PACKAGE_VERSION%.nupkg" 2>nul
del /q "%OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.Testing.%PACKAGE_VERSION%.nupkg" 2>nul


REM ============================================================
REM Pack framework
REM ============================================================

echo.
echo ============================================================
echo Packing Pillaro.Dataverse.PluginFramework
echo ============================================================

"%NUGET_EXE%" pack "%FRAMEWORK_NUSPEC%" ^
  -BasePath "%FRAMEWORK_BASEPATH%" ^
  -OutputDirectory "%OUTPUT_DIR%" ^
  -NonInteractive ^
  -Properties "configuration=%CONFIGURATION%;version=%PACKAGE_VERSION%"

if errorlevel 1 (
    echo.
    echo ERROR: Framework package creation failed.
    exit /b 1
)


REM ============================================================
REM Pack testing framework
REM ============================================================

echo.
echo ============================================================
echo Packing Pillaro.Dataverse.PluginFramework.Testing
echo ============================================================

"%NUGET_EXE%" pack "%TESTING_NUSPEC%" ^
  -BasePath "%TESTING_BASEPATH%" ^
  -OutputDirectory "%OUTPUT_DIR%" ^
  -NonInteractive ^
  -Properties "configuration=%CONFIGURATION%;version=%PACKAGE_VERSION%"

if errorlevel 1 (
    echo.
    echo ERROR: Testing package creation failed.
    exit /b 1
)


REM ============================================================
REM Result
REM ============================================================

echo.
echo ============================================================
echo Done
echo ============================================================
echo Created packages:
echo %OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.%PACKAGE_VERSION%.nupkg
echo %OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.Testing.%PACKAGE_VERSION%.nupkg
echo.

endlocal
exit /b 0