param(
    [Parameter(Mandatory = $true)]
    [string] $SourceDir,

    [Parameter(Mandatory = $true)]
    [string] $ZipPath
)

$ErrorActionPreference = "Stop"

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BasePath,

        [Parameter(Mandatory = $true)]
        [string] $FullPath
    )

    $baseUri = New-Object System.Uri(($BasePath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $fullUri = New-Object System.Uri($FullPath)

    return [System.Uri]::UnescapeDataString(
        $baseUri.MakeRelativeUri($fullUri).ToString()
    ).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Test-IsExcluded {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $segments = $RelativePath -split '[\\/]'

    $excludedDirectories = @(
        "bin",
        "obj",
        ".vs",
        ".git"
    )

    foreach ($segment in $segments) {
        if ($excludedDirectories -contains $segment) {
            return $true
        }
    }

    $fileName = [System.IO.Path]::GetFileName($RelativePath)

    $excludedFilePatterns = @(
        "*.user",
        "*.suo",
        "*.zip",
        "*.vsix"
    )

    foreach ($pattern in $excludedFilePatterns) {
        if ($fileName -like $pattern) {
            return $true
        }
    }

    return $false
}

$sourceFullPath = [System.IO.Path]::GetFullPath($SourceDir)
$zipFullPath = [System.IO.Path]::GetFullPath($ZipPath)
$zipDirectory = [System.IO.Path]::GetDirectoryName($zipFullPath)

if (-not (Test-Path -LiteralPath $sourceFullPath)) {
    throw "Source template directory does not exist: $sourceFullPath"
}

if (-not (Test-Path -LiteralPath $zipDirectory)) {
    New-Item -ItemType Directory -Path $zipDirectory | Out-Null
}

if (Test-Path -LiteralPath $zipFullPath) {
    Remove-Item -LiteralPath $zipFullPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$fileStream = [System.IO.File]::Open(
    $zipFullPath,
    [System.IO.FileMode]::CreateNew,
    [System.IO.FileAccess]::ReadWrite,
    [System.IO.FileShare]::None
)

try {
    $zipArchive = New-Object System.IO.Compression.ZipArchive(
        $fileStream,
        [System.IO.Compression.ZipArchiveMode]::Create
    )

    try {
        # Add directories first, including empty directories.
        $directories = Get-ChildItem -LiteralPath $sourceFullPath -Directory -Recurse -Force |
            Sort-Object FullName

        foreach ($directory in $directories) {
            $relativePath = Get-RelativePath -BasePath $sourceFullPath -FullPath $directory.FullName

            if (Test-IsExcluded -RelativePath $relativePath) {
                continue
            }

            $entryName = ($relativePath -replace '\\', '/').TrimEnd('/') + '/'
            $null = $zipArchive.CreateEntry($entryName)
        }

        # Add files.
        $files = Get-ChildItem -LiteralPath $sourceFullPath -File -Recurse -Force |
            Sort-Object FullName

        foreach ($file in $files) {
            $relativePath = Get-RelativePath -BasePath $sourceFullPath -FullPath $file.FullName

            if (Test-IsExcluded -RelativePath $relativePath) {
                continue
            }

            $entryName = $relativePath -replace '\\', '/'

            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zipArchive,
                $file.FullName,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        $zipArchive.Dispose()
    }
}
finally {
    $fileStream.Dispose()
}

Write-Host "Project template ZIP created:"
Write-Host "  Source: $sourceFullPath"
Write-Host "  ZIP:    $zipFullPath"