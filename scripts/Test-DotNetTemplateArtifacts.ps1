[CmdletBinding()]
param(
    [string]$TemplatePackagePath,
    [string]$TemplateInstallPath,
    [string]$TemplateShortName = 'pillaro-dataverse-plugin',
    [string]$ArtifactsDirectory,
    [string]$Configuration = 'Debug',
    [string]$SmokeProjectName = 'Pill.RemoteSmoke',
    [switch]$SkipBuildSmoke
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')

if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) {
    $ArtifactsDirectory = Join-Path $repoRoot 'artifacts\templates'
}

if ([string]::IsNullOrWhiteSpace($TemplatePackagePath)) {
    $TemplatePackagePath = Join-Path $repoRoot 'templates\Pillaro.Dataverse.PluginTemplate.DotNetNew\bin\Release\Pillaro.Dataverse.PluginTemplate.DotNetNew.1.0.12.nupkg'
}

if ([string]::IsNullOrWhiteSpace($TemplateInstallPath)) {
    $TemplateInstallPath = Join-Path $repoRoot 'templates\Pillaro.Dataverse.PluginTemplate.DotNetNew\template\ProjectTemplate'
}

$artifactsRoot = [System.IO.Path]::GetFullPath($ArtifactsDirectory)
$workspaceArtifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$packageFullPath = [System.IO.Path]::GetFullPath($TemplatePackagePath)
$smokeRoot = Join-Path $artifactsRoot 'template-smoke-dotnetnew'
$generatedRoot = Join-Path $smokeRoot 'generated'

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found: $Path"
    }
}

function Remove-SafeDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($workspaceArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside artifacts: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
}

$dotnetAppDataRoot = Join-Path $workspaceArtifactsRoot 'dotnet-appdata'
$dotnetLocalAppDataRoot = Join-Path $workspaceArtifactsRoot 'dotnet-local-appdata'
Remove-SafeDirectory -Path $dotnetAppDataRoot
Remove-SafeDirectory -Path $dotnetLocalAppDataRoot
New-Item -ItemType Directory -Path $dotnetAppDataRoot -Force | Out-Null
New-Item -ItemType Directory -Path $dotnetLocalAppDataRoot -Force | Out-Null
$env:APPDATA = $dotnetAppDataRoot
$env:LOCALAPPDATA = $dotnetLocalAppDataRoot

function Get-ZipEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $Path))
    $entries = @{}

    foreach ($entry in $zip.Entries) {
        $entries[$entry.FullName.Replace('/', '\')] = $entry
    }

    return @{
        Zip = $zip
        Entries = $entries
    }
}

function Get-MSBuildPath {
    $vswhereCandidates = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    )

    foreach ($vswhere in $vswhereCandidates) {
        if (Test-Path -LiteralPath $vswhere) {
            $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
            if (-not [string]::IsNullOrWhiteSpace($msbuild) -and (Test-Path -LiteralPath $msbuild)) {
                return $msbuild
            }
        }
    }

    $fallbacks = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($fallback in $fallbacks) {
        if (Test-Path -LiteralPath $fallback) {
            return $fallback
        }
    }

    throw 'MSBuild.exe was not found. Install Visual Studio Build Tools or run from a Visual Studio environment.'
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [string]$WorkingDirectory = $repoRoot
    )

    Write-Host "> $FilePath $($Arguments -join ' ')"

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments

        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Test-DotNetTemplatePackage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Assert-PathExists -Path $Path -Description 'Template package'

    $zipInfo = Get-ZipEntries -Path $Path
    $zip = $zipInfo.Zip
    $entries = $zipInfo.Entries

    try {
        $requiredEntries = @(
            'content\Pillaro.Dataverse.PluginTemplate\.template.config\template.json',
            'content\Pillaro.Dataverse.PluginTemplate\Pillaro.Dataverse.PluginTemplate.slnx',
            'content\Pillaro.Dataverse.PluginTemplate\Logic\Pillaro.Dataverse.PluginTemplate.Logic.csproj',
            'content\Pillaro.Dataverse.PluginTemplate\Plugins\Pillaro.Dataverse.PluginTemplate.Plugins.csproj',
            'content\Pillaro.Dataverse.PluginTemplate\Tests\Pillaro.Dataverse.PluginTemplate.Tests.csproj',
            'content\Pillaro.Dataverse.PluginTemplate\.vscode\settings.json',
            'content\Pillaro.Dataverse.PluginTemplate\.vscode\extensions.json'
        )

        $missing = $requiredEntries | Where-Object { -not $entries.ContainsKey($_) }
        if ($missing) {
            throw "Template package is missing required files: $($missing -join ', ')"
        }

        Write-Host "Template package validation passed: $Path"
    }
    finally {
        $zip.Dispose()
    }
}

function Invoke-DotNetNewSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Remove-SafeDirectory -Path $smokeRoot
    New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @('new', 'install', $Path, '--force') -WorkingDirectory $repoRoot

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @(
        'new',
        $TemplateShortName,
        '--name',
        $SmokeProjectName,
        '--output',
        $generatedRoot,
        '--no-update-check'
    ) -WorkingDirectory $repoRoot

    $solution = Get-ChildItem -LiteralPath $generatedRoot -Filter "$SmokeProjectName.slnx" -File | Select-Object -First 1
    if (-not $solution) {
        $solution = Get-ChildItem -LiteralPath $generatedRoot -Filter "$SmokeProjectName.sln" -File | Select-Object -First 1
    }

    if (-not $solution) {
        throw "dotnet new did not create a solution file under $generatedRoot."
    }

    Assert-PathExists -Path (Join-Path $generatedRoot "Logic\$SmokeProjectName.Logic.csproj") -Description 'Generated Logic project'
    Assert-PathExists -Path (Join-Path $generatedRoot "Plugins\$SmokeProjectName.Plugins.csproj") -Description 'Generated Plugins project'
    Assert-PathExists -Path (Join-Path $generatedRoot "Tests\$SmokeProjectName.Tests.csproj") -Description 'Generated Tests project'

    $vscodeSettings = Join-Path $generatedRoot '.vscode\settings.json'
    if (Test-Path -LiteralPath $vscodeSettings) {
        $settingsContent = Get-Content -LiteralPath $vscodeSettings -Raw
        if ($settingsContent -notmatch [Regex]::Escape("$SmokeProjectName.slnx")) {
            throw "VS Code settings were generated but do not reference '$SmokeProjectName.slnx'."
        }
    }

    $namespaceChecks = @(
        @{
            Path = Join-Path $generatedRoot 'Logic\Plugins\ExamplePlugin.cs'
            Expected = "namespace $SmokeProjectName.Logic.Plugins;"
        },
        @{
            Path = Join-Path $generatedRoot 'Logic\Tasks\Example\ExampleTask.cs'
            Expected = "namespace $SmokeProjectName.Logic.Tasks.Example;"
        },
        @{
            Path = Join-Path $generatedRoot 'Tests\TestAutofacModule.cs'
            Expected = "namespace $SmokeProjectName.Tests;"
        },
        @{
            Path = Join-Path $generatedRoot 'Tests\Tests\ConnectionTests.cs'
            Expected = "namespace $SmokeProjectName.Tests.Tests;"
        }
    )

    foreach ($check in $namespaceChecks) {
        Assert-PathExists -Path $check.Path -Description 'Generated template file'
        $content = Get-Content -LiteralPath $check.Path -Raw
        if ($content -notmatch [Regex]::Escape($check.Expected)) {
            throw "Generated file '$($check.Path)' does not contain expected namespace '$($check.Expected)'."
        }

        if ($content -match '\$safeprojectname\$') {
            throw ('Generated file ''' + $check.Path + ''' still contains literal "$safeprojectname$".')
        }
    }

    $projectReferenceChecks = @(
        @{
            Path = Join-Path $generatedRoot "Plugins\$SmokeProjectName.Plugins.csproj"
            Expected = '..\Logic\' + $SmokeProjectName + '.Logic.csproj'
            Description = 'Generated Plugins project reference'
        },
        @{
            Path = Join-Path $generatedRoot "Tests\$SmokeProjectName.Tests.csproj"
            Expected = '..\Logic\' + $SmokeProjectName + '.Logic.csproj'
            Description = 'Generated Tests project reference'
        }
    )

    foreach ($check in $projectReferenceChecks) {
        Assert-PathExists -Path $check.Path -Description $check.Description
        $content = Get-Content -LiteralPath $check.Path -Raw
        if ($content -notmatch [Regex]::Escape($check.Expected)) {
            throw "$($check.Description) does not reference '$($check.Expected)'."
        }
    }

    $folderChecks = @(
        @{
            Path = Join-Path $generatedRoot "Tests\$SmokeProjectName.Tests.csproj"
            Expected = @(
                '<Folder Include="Data\" />',
                '<Folder Include="Data\CleanupHandlers\" />',
                '<Folder Include="Data\Repositories\" />'
            )
            Description = 'Generated Tests folder declarations'
        }
    )

    foreach ($check in $folderChecks) {
        Assert-PathExists -Path $check.Path -Description $check.Description
        $content = Get-Content -LiteralPath $check.Path -Raw
        foreach ($expected in $check.Expected) {
            if ($content -notmatch [Regex]::Escape($expected)) {
                throw "$($check.Description) does not include '$expected'."
            }
        }
    }

    if (-not $SkipBuildSmoke) {
        $msbuild = Get-MSBuildPath
        Invoke-CheckedCommand -FilePath $msbuild -Arguments @(
            $solution.FullName,
            '/t:Restore,Build',
            "/p:Configuration=$Configuration",
            '/p:UseSharedCompilation=false',
            '/m:1'
        ) -WorkingDirectory $generatedRoot
    }

    Write-Host "dotnet new smoke build passed: $($solution.FullName)"
}

Test-DotNetTemplatePackage -Path $packageFullPath
Invoke-DotNetNewSmoke -Path $TemplateInstallPath

Write-Host 'Dotnet template artifact validation passed.'
