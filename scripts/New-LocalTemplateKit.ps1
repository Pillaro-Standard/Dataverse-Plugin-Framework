<#
.SYNOPSIS
    Builds a local test kit: framework and testing packages plus a dotnet new template that uses
    them, for trying unreleased changes in a fresh solution exactly as a user would.

.DESCRIPTION
    Output (git-ignored), in artifacts/local-kit/<Version>/:
      packages/     Pillaro.Dataverse.PluginFramework, .Testing and the dotnet new template package
      nuget.config  a package source for packages/, to copy next to the new solution
    The template is stamped with -Version in a copy of templates/, so no tracked file changes.

.EXAMPLE
    ./scripts/New-LocalTemplateKit.ps1 -Version 1.3.0-ai.1
#>
param(
    [string]$Version = '1.3.0-local.1',
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$kitRoot = Join-Path $repositoryRoot "artifacts\local-kit\$Version"
$packages = Join-Path $kitRoot 'packages'
$work = Join-Path $kitRoot 'work'

function Invoke-Step([string]$name, [scriptblock]$command) {
    Write-Host "== $name"
    & $command
    if ($LASTEXITCODE -ne 0) {
        throw "$name failed (exit code $LASTEXITCODE)."
    }
}


if (Test-Path $kitRoot) {
    Remove-Item -LiteralPath $kitRoot -Recurse -Force
}
New-Item -ItemType Directory -Force $packages | Out-Null

$versionProperties = @("-p:Version=$Version", "-p:InformationalVersion=$Version", '-p:IncludeSourceRevisionInInformationalVersion=false')

Invoke-Step 'Publish the CLI' {
    dotnet publish (Join-Path $repositoryRoot 'tools\Pillaro.Dataverse.PluginFramework.Cli\Pillaro.Dataverse.PluginFramework.Cli.csproj') `
        -c $Configuration -f net8.0 --no-self-contained --nologo -v quiet @versionProperties
}

Invoke-Step 'Build the framework' {
    dotnet build (Join-Path $repositoryRoot 'src\Pillaro.Dataverse.PluginFramework\Pillaro.Dataverse.PluginFramework.csproj') `
        -c $Configuration --nologo -v quiet @versionProperties
}

Invoke-Step 'Build the testing package' {
    dotnet build (Join-Path $repositoryRoot 'src\Pillaro.Dataverse.PluginFramework.Testing\Pillaro.Dataverse.PluginFramework.Testing.csproj') `
        -c $Configuration --nologo -v quiet @versionProperties
}

# The same .nuspec files CI packs with nuget.exe, packed through the SDK's NuGet, so no separate
# nuget.exe (of a version that knows every element the .nuspec uses) is needed.
foreach ($package in @(
        @{ Project = 'src\Pillaro.Dataverse.PluginFramework\Pillaro.Dataverse.PluginFramework.csproj'; Nuspec = 'src\Pillaro.Dataverse.PluginFramework\Tools\PluginPackaging\Pillaro.Dataverse.PluginFramework.nuspec'; BasePath = 'src\Pillaro.Dataverse.PluginFramework' },
        @{ Project = 'src\Pillaro.Dataverse.PluginFramework.Testing\Pillaro.Dataverse.PluginFramework.Testing.csproj'; Nuspec = 'src\Pillaro.Dataverse.PluginFramework.Testing\Tools\TestingPackaging\Pillaro.Dataverse.PluginFramework.Testing.nuspec'; BasePath = 'src\Pillaro.Dataverse.PluginFramework.Testing' })) {
    # $version$ and $configuration$ are filled in a copy: passing NuspecProperties with its ';'
    # through the command line is not reliable.
    $nuspec = Join-Path $work ([IO.Path]::GetFileName($package.Nuspec))
    New-Item -ItemType Directory -Force $work | Out-Null
    [IO.File]::WriteAllText($nuspec, [IO.File]::ReadAllText((Join-Path $repositoryRoot $package.Nuspec)).Replace('$version$', $Version).Replace('$configuration$', $Configuration))

    Invoke-Step "Pack $([IO.Path]::GetFileNameWithoutExtension($package.Nuspec))" {
        dotnet pack (Join-Path $repositoryRoot $package.Project) -c $Configuration --no-build --nologo -v quiet @versionProperties `
            -o $packages "-p:NuspecFile=$nuspec" "-p:NuspecBasePath=$(Join-Path $repositoryRoot $package.BasePath)"
    }
}

# Stamp a copy of the templates, so the tracked template project files keep their version.
Write-Host '== Stamp a copy of the templates'
$workTemplates = Join-Path $work 'templates'
robocopy (Join-Path $repositoryRoot 'templates') $workTemplates /E /XD bin obj .vs /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "Copying the templates failed (robocopy exit code $LASTEXITCODE)."
}
& (Join-Path $PSScriptRoot 'Set-TemplateFrameworkVersion.ps1') -FrameworkVersion $Version -RepositoryRoot $work | Out-Null

Invoke-Step 'Pack the dotnet new template' {
    dotnet build (Join-Path $workTemplates 'Pillaro.Dataverse.PluginTemplate.DotNetNew\Pillaro.Dataverse.PluginTemplate.DotNetNew.csproj') `
        -c Release --nologo -v quiet
}
Copy-Item (Join-Path $workTemplates 'Pillaro.Dataverse.PluginTemplate.DotNetNew\bin\Release\*.nupkg') $packages

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="pillaro-local-kit" value="$packages" />
  </packageSources>
</configuration>
"@ | Set-Content -Path (Join-Path $kitRoot 'nuget.config') -Encoding utf8

Remove-Item -LiteralPath $work -Recurse -Force

$template = Get-ChildItem $packages -Filter 'Pillaro.Dataverse.PluginTemplate.DotNetNew.*.nupkg' | Select-Object -First 1
Write-Host ''
Write-Host "Kit ready: $kitRoot"
Write-Host ''
Write-Host 'Create a solution from it:'
Write-Host "  dotnet new install `"$($template.FullName)`" --force"
Write-Host '  dotnet new pillaro-dataverse-plugin-dotnet --name <Name> --output <folder>'
Write-Host "  copy `"$(Join-Path $kitRoot 'nuget.config')`" <folder>"
Write-Host '  dotnet build <folder>'
