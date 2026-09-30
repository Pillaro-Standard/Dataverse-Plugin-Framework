<#
.SYNOPSIS
    Writes the AI instructions into the project template's source, so a solution created with
    dotnet new has them from the start.

.DESCRIPTION
    Runs 'pillaro-dv ai-sync' against templates/Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate.
    The snapshot is committed. Run this script after changing anything under docs/ai/,
    .github/instructions/ or ai/, and commit the result. -Check (used by the PR build) only
    reports whether the committed snapshot is out of date.

.PARAMETER Check
    Change nothing; fail when the snapshot is out of date.

.PARAMETER Configuration
    Build configuration of the CLI.

.PARAMETER NoBuild
    Use the CLI already built in that configuration.
#>
param(
    [switch]$Check,
    [string]$Configuration = 'Debug',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repositoryRoot 'tools\Pillaro.Dataverse.PluginFramework.Cli\Pillaro.Dataverse.PluginFramework.Cli.csproj'
$cliDll = Join-Path $repositoryRoot "tools\Pillaro.Dataverse.PluginFramework.Cli\bin\$Configuration\net8.0\pillaro-dv.dll"
$templateRoot = Join-Path $repositoryRoot 'templates\Pillaro.Dataverse.PluginTemplate.Source\ProjectTemplate'

if (-not $NoBuild) {
    dotnet build $cliProject -c $Configuration -v quiet -nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'Building the CLI failed.'
    }
}

if (-not (Test-Path $cliDll)) {
    throw "CLI not found at $cliDll. Build it first, or run without -NoBuild."
}

# The template's projects are Logic/ and Tests/; their .csproj files live in the template packages,
# not in the shared source, so they are passed explicitly.
$arguments = @('ai-sync', '--root', $templateRoot, '--logic-project', 'Logic', '--tests-project', 'Tests')
if ($Check) {
    $arguments += '--check'
}

dotnet $cliDll @arguments
$exitCode = $LASTEXITCODE

if ($Check -and $exitCode -eq 3) {
    Write-Error 'The AI instructions in the template are out of date. Run scripts/Update-TemplateAiInstructions.ps1 and commit the result.'
}

exit $exitCode
