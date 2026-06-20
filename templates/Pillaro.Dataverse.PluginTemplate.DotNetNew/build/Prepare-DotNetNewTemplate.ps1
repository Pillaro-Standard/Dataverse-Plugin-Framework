[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRoot,

    [Parameter(Mandatory = $true)]
    [string]$DestinationRoot
)

$ErrorActionPreference = 'Stop'

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $normalized = $Content -replace "`r`n|`n|`r", "`r`n"
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

function Update-TextFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [hashtable[]]$Replacements
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Template file was not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    foreach ($replacement in $Replacements) {
        $content = $content.Replace($replacement.From, $replacement.To)
    }

    Write-Utf8File -Path $Path -Content $content
}

function Assert-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Directory was not found: $Path"
    }
}

Assert-Directory -Path $SourceRoot

if (Test-Path -LiteralPath $DestinationRoot) {
    Remove-Item -LiteralPath $DestinationRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
Copy-Item -Path (Join-Path $SourceRoot '*') -Destination $DestinationRoot -Recurse -Force

$iconPath = Join-Path $DestinationRoot 'PillaroLogo128.png'
if (Test-Path -LiteralPath $iconPath) {
    Remove-Item -LiteralPath $iconPath -Force
}

$transformations = @{
    'Logic\README.md' = @(
        @{
            From = '1. Create a new solution from the Visual Studio template.'
            To   = '1. Create a new solution from the dotnet new template.'
        },
        @{
            From = '2. Run **Build > Rebuild Solution**.'
            To   = '2. Run **Build > Rebuild Solution** in Visual Studio, or run `dotnet build` from the terminal in Visual Studio Code.'
        }
    )
    'Logic\Plugins\ExamplePlugin.cs' = @(
        @{
            From = 'using $safeprojectname$.Tasks.Example;'
            To   = 'using $safeprojectname$.Logic.Tasks.Example;'
        },
        @{
            From = 'namespace $safeprojectname$.Plugins;'
            To   = 'namespace $safeprojectname$.Logic.Plugins;'
        }
    )
    'Logic\Plugins\PluginBase.cs' = @(
        @{
            From = 'namespace $safeprojectname$.Plugins;'
            To   = 'namespace $safeprojectname$.Logic.Plugins;'
        }
    )
    'Logic\Tasks\Example\ExampleTask.cs' = @(
        @{
            From = 'namespace $safeprojectname$.Tasks.Example;'
            To   = 'namespace $safeprojectname$.Logic.Tasks.Example;'
        }
    )
    'Tests\TestAutofacModule.cs' = @(
        @{
            From = 'namespace $safeprojectname$;'
            To   = 'namespace $safeprojectname$.Tests;'
        }
    )
    'Tests\Tests\TestBase.cs' = @(
        @{
            From = 'using Pillaro.Dataverse.PluginFramework.Testing.Tests;'
            To   = ('using Pillaro.Dataverse.PluginFramework.Testing.Tests;' + [Environment]::NewLine + 'using $safeprojectname$.Tests;')
        },
        @{
            From = 'namespace $safeprojectname$.Tests;'
            To   = 'namespace $safeprojectname$.Tests.Tests;'
        }
    )
    'Tests\Tests\ConnectionTests.cs' = @(
        @{
            From = 'using Pillaro.Dataverse.PluginFramework.Testing.Tests;'
            To   = ('using Pillaro.Dataverse.PluginFramework.Testing.Tests;' + [Environment]::NewLine + 'using $safeprojectname$.Tests;')
        },
        @{
            From = 'namespace $safeprojectname$.Tests;'
            To   = 'namespace $safeprojectname$.Tests.Tests;'
        }
    )
}

foreach ($relativePath in $transformations.Keys) {
    $fullPath = Join-Path $DestinationRoot $relativePath
    Update-TextFile -Path $fullPath -Replacements $transformations[$relativePath]
}

Write-Host "Prepared dotnet new template staging folder: $DestinationRoot"
