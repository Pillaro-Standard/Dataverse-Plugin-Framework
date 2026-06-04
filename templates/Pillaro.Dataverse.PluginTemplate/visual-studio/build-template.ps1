[CmdletBinding()]
param(
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$templateName = 'Pillaro.Dataverse.PluginTemplate'
$templateRoot = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $templateRoot '..\..\..')
$canonicalRoot = Join-Path $repoRoot "templates\$templateName"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\templates'
}

$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$stagingRoot = Join-Path $artifactsRoot "obj\$templateName.VisualStudio.Template"
$zipPath = Join-Path $outputRoot "$templateName.zip"

if (-not $stagingRoot.StartsWith($artifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean staging path outside artifacts: $stagingRoot"
}

function Copy-TemplateDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,
        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    $sourceRoot = [System.IO.Path]::GetFullPath($Source).TrimEnd('\')
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Force |
        Where-Object {
            $relativePath = $_.FullName.Substring($sourceRoot.Length).TrimStart('\')
            $pathParts = $relativePath -split '\\'
            $pathParts -notcontains 'bin' -and $pathParts -notcontains 'obj'
        } |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($sourceRoot.Length).TrimStart('\')
            $targetPath = Join-Path $Destination $relativePath
            $targetDirectory = Split-Path -Parent $targetPath

            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
        }
}

function Replace-InTemplateFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [hashtable]$Replacements
    )

    Get-ChildItem -LiteralPath $Path -Recurse -File -Force |
        Where-Object { $_.Extension -in @('.cs', '.csproj') } |
        ForEach-Object {
            $content = Get-Content -LiteralPath $_.FullName -Raw

            foreach ($key in $Replacements.Keys) {
                $content = $content.Replace($key, $Replacements[$key])
            }

            Set-Content -LiteralPath $_.FullName -Value $content -Encoding UTF8
        }
}

if (Test-Path $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $templateRoot "$templateName.vstemplate") -Destination $stagingRoot -Force

$projectMappings = @(
    @{
        Source = Join-Path $canonicalRoot "$templateName.Logic"
        Destination = Join-Path $stagingRoot 'Logic'
        Vstemplate = Join-Path $templateRoot 'Logic\Logic.vstemplate'
    },
    @{
        Source = Join-Path $canonicalRoot "$templateName.Plugins"
        Destination = Join-Path $stagingRoot 'Plugins'
        Vstemplate = Join-Path $templateRoot 'Plugins\Plugins.vstemplate'
    },
    @{
        Source = Join-Path $canonicalRoot "$templateName.Tests"
        Destination = Join-Path $stagingRoot 'Tests'
        Vstemplate = Join-Path $templateRoot 'Tests\Tests.vstemplate'
    }
)

foreach ($mapping in $projectMappings) {
    Copy-TemplateDirectory -Source $mapping.Source -Destination $mapping.Destination
    Copy-Item -LiteralPath $mapping.Vstemplate -Destination $mapping.Destination -Force
}

Replace-InTemplateFiles -Path (Join-Path $stagingRoot 'Logic') -Replacements @{
    'Pillaro.Dataverse.PluginTemplate.Logic' = '$safeprojectname$'
}

Replace-InTemplateFiles -Path (Join-Path $stagingRoot 'Plugins') -Replacements @{
    'Pillaro.Dataverse.PluginTemplate.Plugins' = '$safeprojectname$'
    '..\Pillaro.Dataverse.PluginTemplate.Logic\Pillaro.Dataverse.PluginTemplate.Logic.csproj' = '..\$ext_safeprojectname$.Logic\$ext_safeprojectname$.Logic.csproj'
    'Pillaro.Dataverse.PluginTemplate.Logic.dll' = '$ext_safeprojectname$.Logic.dll'
}

Replace-InTemplateFiles -Path (Join-Path $stagingRoot 'Tests') -Replacements @{
    'Pillaro.Dataverse.PluginTemplate.Tests' = '$safeprojectname$'
    '..\Pillaro.Dataverse.PluginTemplate.Logic\Pillaro.Dataverse.PluginTemplate.Logic.csproj' = '..\$ext_safeprojectname$.Logic\$ext_safeprojectname$.Logic.csproj'
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $stagingRoot '*') -DestinationPath $zipPath -Force
Remove-Item -LiteralPath $stagingRoot -Recurse -Force

Write-Host "Visual Studio template package created: $zipPath"
