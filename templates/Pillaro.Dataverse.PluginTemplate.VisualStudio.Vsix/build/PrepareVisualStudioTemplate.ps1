param(
    [Parameter(Mandatory = $true)]
    [string] $SharedSourceDir,

    [Parameter(Mandatory = $true)]
    [string] $VisualStudioOverlayDir,

    [Parameter(Mandatory = $true)]
    [string] $PreparedTemplateDir
)

$ErrorActionPreference = "Stop"

function Copy-DirectoryContent {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourceDir,

        [Parameter(Mandatory = $true)]
        [string] $TargetDir,

        [string[]] $ExcludedExtensions = @()
    )

    $sourceFullPath = [System.IO.Path]::GetFullPath($SourceDir)
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetDir)

    if (-not (Test-Path -LiteralPath $sourceFullPath)) {
        throw "Source directory does not exist: $sourceFullPath"
    }

    $files = Get-ChildItem -LiteralPath $sourceFullPath -File -Recurse -Force

    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($sourceFullPath.Length).TrimStart('\', '/')
        $segments = $relativePath -split '[\\/]'

        if ($segments -contains "bin" -or $segments -contains "obj" -or $segments -contains ".vs") {
            continue
        }

        if ($ExcludedExtensions -contains $file.Extension) {
            continue
        }

        $destinationPath = Join-Path $targetFullPath $relativePath
        $destinationDirectory = Split-Path -Parent $destinationPath

        if (-not (Test-Path -LiteralPath $destinationDirectory)) {
            New-Item -ItemType Directory -Path $destinationDirectory | Out-Null
        }

        Copy-Item -LiteralPath $file.FullName -Destination $destinationPath -Force
    }
}

$sharedSourceFullPath = [System.IO.Path]::GetFullPath($SharedSourceDir)
$visualStudioOverlayFullPath = [System.IO.Path]::GetFullPath($VisualStudioOverlayDir)
$preparedTemplateFullPath = [System.IO.Path]::GetFullPath($PreparedTemplateDir)

if (-not (Test-Path -LiteralPath $sharedSourceFullPath)) {
    throw "Shared source directory does not exist: $sharedSourceFullPath"
}

if (-not (Test-Path -LiteralPath $visualStudioOverlayFullPath)) {
    throw "Visual Studio overlay directory does not exist: $visualStudioOverlayFullPath"
}

if (Test-Path -LiteralPath $preparedTemplateFullPath) {
    Remove-Item -LiteralPath $preparedTemplateFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $preparedTemplateFullPath | Out-Null

Copy-DirectoryContent `
    -SourceDir $sharedSourceFullPath `
    -TargetDir $preparedTemplateFullPath `
    -ExcludedExtensions @(".csproj", ".vstemplate")

Copy-DirectoryContent `
    -SourceDir $visualStudioOverlayFullPath `
    -TargetDir $preparedTemplateFullPath

Write-Host "Visual Studio template prepared:"
Write-Host "  Shared source path: $sharedSourceFullPath"
Write-Host "  Visual Studio overlay path: $visualStudioOverlayFullPath"
Write-Host "  Prepared template path: $preparedTemplateFullPath"
