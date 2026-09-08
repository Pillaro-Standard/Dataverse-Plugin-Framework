[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$FrameworkVersion,

    [Parameter(Mandatory = $false)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'

if ($FrameworkVersion -notmatch '^\d+\.\d+\.\d+([\-+].*)?$') {
    throw "FrameworkVersion '$FrameworkVersion' is not a valid NuGet version. Expected Major.Minor.Patch with an optional prerelease suffix."
}

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}

$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path

# Overlay project files for both delivery formats. The generated-project csproj files are
# owned by the packaging projects, not by the shared template source, so both overlays
# have to be stamped. Paths are listed explicitly so a moved or renamed file fails the
# build instead of silently going unstamped.
$relativePaths = @(
    'templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/template/ProjectTemplate/Logic/Pillaro.Dataverse.PluginTemplate.Logic.csproj',
    'templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/template/ProjectTemplate/Plugins/Pillaro.Dataverse.PluginTemplate.Plugins.csproj',
    'templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/template/ProjectTemplate/Tests/Pillaro.Dataverse.PluginTemplate.Tests.csproj',
    'templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/template/ProjectTemplates/Pillaro.Dataverse.PluginTemplate/Logic/Pillaro.Dataverse.PluginTemplate.Logic.csproj',
    'templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/template/ProjectTemplates/Pillaro.Dataverse.PluginTemplate/Plugins/Pillaro.Dataverse.PluginTemplate.Plugins.csproj',
    'templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/template/ProjectTemplates/Pillaro.Dataverse.PluginTemplate/Tests/Pillaro.Dataverse.PluginTemplate.Tests.csproj'
)

# The closing quote after the package id keeps the framework pattern from also matching
# the Testing package.
$packageIds = @(
    'Pillaro.Dataverse.PluginFramework',
    'Pillaro.Dataverse.PluginFramework.Testing'
)

$totalReplacements = 0

foreach ($relativePath in $relativePaths) {
    $path = Join-Path $RepositoryRoot $relativePath

    if (-not (Test-Path -LiteralPath $path)) {
        throw "Template project file was not found: $path"
    }

    $bytes = [System.IO.File]::ReadAllBytes($path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $content = [System.Text.Encoding]::UTF8.GetString($bytes)

    if ($hasBom) {
        $content = $content.Substring(1)
    }

    $original = $content
    $fileReplacements = 0

    foreach ($packageId in $packageIds) {
        $pattern = '(Include="' + [regex]::Escape($packageId) + '"\s+Version=")[^"]*(")'
        $matchCount = ([regex]::Matches($content, $pattern)).Count

        if ($matchCount -eq 0) {
            continue
        }

        $content = [regex]::Replace($content, $pattern, "`${1}$FrameworkVersion`${2}")
        $fileReplacements += $matchCount
    }

    if ($fileReplacements -eq 0) {
        throw "No Pillaro package reference was found to stamp in '$relativePath'. The template project layout changed and this script needs updating."
    }

    if ($content -ne $original) {
        $encoding = New-Object System.Text.UTF8Encoding -ArgumentList $hasBom
        [System.IO.File]::WriteAllText($path, $content, $encoding)
    }

    $totalReplacements += $fileReplacements
    Write-Host "Stamped $fileReplacements reference(s) in $relativePath"
}

# Guard against a partial stamp leaving a mix of versions behind.
foreach ($relativePath in $relativePaths) {
    $path = Join-Path $RepositoryRoot $relativePath
    $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

    foreach ($packageId in $packageIds) {
        $pattern = 'Include="' + [regex]::Escape($packageId) + '"\s+Version="([^"]*)"'

        foreach ($match in [regex]::Matches($content, $pattern)) {
            $actual = $match.Groups[1].Value

            if ($actual -ne $FrameworkVersion) {
                throw "Verification failed: '$relativePath' still references $packageId version '$actual' instead of '$FrameworkVersion'."
            }
        }
    }
}

Write-Host "Stamped framework version '$FrameworkVersion' into $totalReplacements package reference(s) across $($relativePaths.Count) template project file(s)."
