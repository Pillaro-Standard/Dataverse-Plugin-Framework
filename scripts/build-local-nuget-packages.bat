@echo off
setlocal EnableExtensions

REM ============================================================
REM Build local NuGet packages for Pillaro Dataverse Plugin Framework
REM ============================================================
REM Usage:
REM - place this file in the repository root
REM - run it from cmd or double-click it
REM - enter package version when prompted
REM
REM Output:
REM - C:\LocalNuGet\Pillaro.Dataverse.PluginFramework.<version>.nupkg
REM - C:\LocalNuGet\Pillaro.Dataverse.PluginFramework.Testing.<version>.nupkg
REM ============================================================

set "ROOT=%~dp0"
set "CONFIGURATION=Debug"
set "TARGET_FRAMEWORK=net8.0"
set "OUTPUT_DIR=C:\LocalNuGet"

set "NUGET_EXE=%ROOT%tools\nuget.exe"

set "CLI_PROJECT=%ROOT%tools\Pillaro.Dataverse.PluginFramework.Cli\Pillaro.Dataverse.PluginFramework.Cli.csproj"

set "FRAMEWORK_PROJECT=%ROOT%src\Pillaro.Dataverse.PluginFramework\Pillaro.Dataverse.PluginFramework.csproj"
set "FRAMEWORK_NUSPEC=%ROOT%src\Pillaro.Dataverse.PluginFramework\Tools\PluginPackaging\Pillaro.Dataverse.PluginFramework.nuspec"
set "FRAMEWORK_BASEPATH=%ROOT%src\Pillaro.Dataverse.PluginFramework"

set "TESTING_PROJECT=%ROOT%src\Pillaro.Dataverse.PluginFramework.Testing\Pillaro.Dataverse.PluginFramework.Testing.csproj"
set "TESTING_NUSPEC=%ROOT%src\Pillaro.Dataverse.PluginFramework.Testing\Tools\TestingPackaging\Pillaro.Dataverse.PluginFramework.Testing.nuspec"
set "TESTING_BASEPATH=%ROOT%src\Pillaro.Dataverse.PluginFramework.Testing"

set "DEFAULT_VERSION=0.0.6-local"

echo.
echo Pillaro Dataverse Plugin Framework local package build
echo ------------------------------------------------------------
echo Default version: %DEFAULT_VERSION%
echo.

set /p PACKAGE_VERSION=Enter package version, or press ENTER to use default: 

if "%PACKAGE_VERSION%"=="" (
    set "PACKAGE_VERSION=%DEFAULT_VERSION%"
)

echo.
echo Version:       %PACKAGE_VERSION%
echo Configuration: %CONFIGURATION%
echo Output:        %OUTPUT_DIR%
echo.

if not exist "%NUGET_EXE%" (
    echo ERROR: nuget.exe was not found:
    echo %NUGET_EXE%
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

if not exist "%TESTING_PROJECT%" (
    echo ERROR: Testing project was not found:
    echo %TESTING_PROJECT%
    exit /b 1
)

if not exist "%TESTING_NUSPEC%" (
    echo ERROR: Testing nuspec was not found:
    echo %TESTING_NUSPEC%
    echo.
    echo Update TESTING_NUSPEC in this .bat file if the nuspec is stored in a different folder.
    exit /b 1
)

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)

echo.
echo ============================================================
echo Publishing CLI
echo ============================================================

dotnet publish "%CLI_PROJECT%" ^
  -c %CONFIGURATION% ^
  -f %TARGET_FRAMEWORK% ^
  --no-self-contained ^
  --nologo

if errorlevel 1 exit /b 1

echo.
echo ============================================================
echo Building framework project
echo ============================================================

dotnet build "%FRAMEWORK_PROJECT%" ^
  -c %CONFIGURATION% ^
  --nologo

if errorlevel 1 exit /b 1

echo.
echo ============================================================
echo Building testing project
echo ============================================================

dotnet build "%TESTING_PROJECT%" ^
  -c %CONFIGURATION% ^
  --nologo

if errorlevel 1 exit /b 1

echo.
echo ============================================================
echo Cleaning previous local packages with the same version
echo ============================================================

del "%OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.%PACKAGE_VERSION%.nupkg" 2>nul
del "%OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.Testing.%PACKAGE_VERSION%.nupkg" 2>nul

echo.
echo ============================================================
echo Packing Pillaro.Dataverse.PluginFramework
echo ============================================================

"%NUGET_EXE%" pack "%FRAMEWORK_NUSPEC%" ^
  -BasePath "%FRAMEWORK_BASEPATH%" ^
  -OutputDirectory "%OUTPUT_DIR%" ^
  -NonInteractive ^
  -Properties "configuration=%CONFIGURATION%;version=%PACKAGE_VERSION%"

if errorlevel 1 exit /b 1

echo.
echo ============================================================
echo Packing Pillaro.Dataverse.PluginFramework.Testing
echo ============================================================

"%NUGET_EXE%" pack "%TESTING_NUSPEC%" ^
  -BasePath "%TESTING_BASEPATH%" ^
  -OutputDirectory "%OUTPUT_DIR%" ^
  -NonInteractive ^
  -Properties "configuration=%CONFIGURATION%;version=%PACKAGE_VERSION%"

if errorlevel 1 exit /b 1

echo.
echo ============================================================
echo Done
echo ============================================================
echo Created packages:
echo %OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.%PACKAGE_VERSION%.nupkg
echo %OUTPUT_DIR%\Pillaro.Dataverse.PluginFramework.Testing.%PACKAGE_VERSION%.nupkg
echo.

endlocal
