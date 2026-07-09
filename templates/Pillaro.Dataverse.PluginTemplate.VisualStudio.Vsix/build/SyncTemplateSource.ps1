param(
    [Parameter(Mandatory = $true)]
    [string] $SourceDir,

    [Parameter(Mandatory = $true)]
    [string] $TargetDir
)

$ErrorActionPreference = "Stop"

$sourceFullPath = [System.IO.Path]::GetFullPath($SourceDir)
$targetFullPath = [System.IO.Path]::GetFullPath($TargetDir)

if (-not (Test-Path -LiteralPath $sourceFullPath)) {
    throw "Template source directory does not exist: $sourceFullPath"
}

if (-not (Test-Path -LiteralPath $targetFullPath)) {
    New-Item -ItemType Directory -Path $targetFullPath | Out-Null
}

$excludedDirectories = @(
    "bin",
    "obj",
    ".vs"
)

$excludedExtensions = @(
    ".csproj",
    ".vstemplate"
)

$files = Get-ChildItem -LiteralPath $sourceFullPath -File -Recurse -Force

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($sourceFullPath.Length).TrimStart('\', '/')
    $segments = $relativePath -split '[\\/]'

    $isExcludedDirectory = $false

    foreach ($segment in $segments) {
        if ($excludedDirectories -contains $segment) {
            $isExcludedDirectory = $true
            break
        }
    }

    if ($isExcludedDirectory) {
        continue
    }

    if ($excludedExtensions -contains $file.Extension) {
        continue
    }

    $destinationPath = Join-Path $targetFullPath $relativePath
    $destinationDirectory = Split-Path -Parent $destinationPath

    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory | Out-Null
    }

    Copy-Item -LiteralPath $file.FullName -Destination $destinationPath -Force
}

Write-Host "Template source synchronized:"
Write-Host "  Source: $sourceFullPath"
Write-Host "  Target: $targetFullPath"