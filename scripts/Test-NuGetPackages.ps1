[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageVersion,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedReleaseNotes,

    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [Parameter(Mandatory = $true)]
    [string]$SourceCommit
)

$ErrorActionPreference = 'Stop'

# Lifted verbatim from the "Verify generated NuGet packages" step of the Azure DevOps
# "Packages – Build & Package" pipeline, with its three pipeline macros turned into
# parameters. Extracted mechanically rather than retyped, so the checks are unchanged.

Add-Type -AssemblyName System.IO.Compression.FileSystem

$packageVersion = $PackageVersion
$expectedReleaseNotes = $ExpectedReleaseNotes
$packages = Get-ChildItem $PackageDirectory -Filter *.nupkg

if (-not $packages) {
  throw "No .nupkg files were found in $PackageDirectory."
}

function Read-ZipEntryText {
  param(
    [System.IO.Compression.ZipArchiveEntry] $Entry
  )

  $stream = $Entry.Open()
  try {
    $reader = New-Object System.IO.StreamReader -ArgumentList $stream, ([System.Text.Encoding]::UTF8), $true
    try {
      return $reader.ReadToEnd()
    }
    finally {
      $reader.Dispose()
    }
  }
  finally {
    $stream.Dispose()
  }
}

function Assert-PackageEntry {
  param(
    [System.IO.Compression.ZipArchive] $Zip,
    [string] $PackageName,
    [string] $EntryPath
  )

  if (-not ($Zip.Entries | Where-Object { $_.FullName -eq $EntryPath })) {
    throw "Package '$PackageName' does not contain required file '$EntryPath'."
  }
}

foreach ($package in $packages) {
  Write-Host "Verifying NuGet package: $($package.FullName)"
  $zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)

  try {
    $entries = $zip.Entries
    $nuspecEntry = $entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1

    if (-not $nuspecEntry) {
      throw "Package '$($package.Name)' does not contain a .nuspec file."
    }

    [xml]$nuspec = Read-ZipEntryText -Entry $nuspecEntry
    $id = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']").InnerText
    $version = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']").InnerText
    $releaseNotesNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='releaseNotes']")

    Write-Host "  PackageId: $id"
    Write-Host "  Version: $version"

    if ($version -ne $packageVersion) {
      throw "Package '$($package.Name)' has nuspec version '$version', expected '$packageVersion'."
    }

    if (-not $releaseNotesNode) {
      throw "Package '$($package.Name)' does not contain a releaseNotes element in the embedded nuspec."
    }

    $actualReleaseNotes = $releaseNotesNode.InnerText
    Write-Host "  ReleaseNotes: $actualReleaseNotes"

    $sourceCommitReleaseNotes = "https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/$SourceCommit/CHANGELOG.md"

    if ($actualReleaseNotes -eq $sourceCommitReleaseNotes) {
      throw "Package '$($package.Name)' has invalid releaseNotes '$actualReleaseNotes'. It must point to the source branch or tag, not the exact source commit."
    }

    if ($actualReleaseNotes -ne $expectedReleaseNotes) {
      throw "Package '$($package.Name)' has releaseNotes '$actualReleaseNotes', expected '$expectedReleaseNotes'."
    }

    if ($id -eq "Pillaro.Dataverse.PluginFramework") {
      $requiredEntries = @(
        "lib/net462/Pillaro.Dataverse.PluginFramework.dll",
        "lib/net462/Pillaro.Dataverse.PluginFramework.pdb",
        "lib/net8.0/Pillaro.Dataverse.PluginFramework.dll",
        "lib/net8.0/Pillaro.Dataverse.PluginFramework.pdb",
        "tools/Deployment/pillaro-dv/pillaro-dv.dll",
        "tools/Deployment/pillaro-dv/pillaro-dv.pdb",
        "tools/Deployment/pillaro-dv/Pillaro.Dataverse.PluginFramework.dll",
        "tools/Deployment/pillaro-dv/Pillaro.Dataverse.PluginFramework.pdb"
      )

      foreach ($entryPath in $requiredEntries) {
        Assert-PackageEntry -Zip $zip -PackageName $package.Name -EntryPath $entryPath
      }

      $readmeEntry = $entries | Where-Object { $_.FullName -eq "tools/ILMerge/README.md" } | Select-Object -First 1

      if (-not $readmeEntry) {
        throw "Package '$($package.Name)' does not contain tools/ILMerge/README.md."
      }

      $readme = Read-ZipEntryText -Entry $readmeEntry
      if ($readme.Contains([string][char]0xFFFD)) {
        throw "Package '$($package.Name)' contains tools/ILMerge/README.md with U+FFFD replacement characters."
      }

      $expectedCli = "tools/Deployment/pillaro-dv/pillaro-dv.dll"
      if (-not ($entries | Where-Object { $_.FullName -eq $expectedCli })) {
        throw "Package '$($package.Name)' does not contain expected CLI file '$expectedCli'."
      }

      $unexpectedCliEntries = $entries | Where-Object {
        $_.FullName -match '^tools/Deployment/.+Pillaro\.Dataverse\.PluginFramework\.Cli/.+'
      }

      if ($unexpectedCliEntries) {
        $paths = ($unexpectedCliEntries | Select-Object -ExpandProperty FullName) -join ', '
        throw "Package '$($package.Name)' contains CLI files under unexpected nested project folder(s): $paths"
      }

      Write-Host "  Framework package content checks passed."
    }
    elseif ($id -eq "Pillaro.Dataverse.PluginFramework.Testing") {
      $requiredEntries = @(
        "lib/net8.0/Pillaro.Dataverse.PluginFramework.Testing.dll",
        "lib/net8.0/Pillaro.Dataverse.PluginFramework.Testing.pdb"
      )

      foreach ($entryPath in $requiredEntries) {
        Assert-PackageEntry -Zip $zip -PackageName $package.Name -EntryPath $entryPath
      }

      $environmentVariablesDependency = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='dependencies']/*[local-name()='group'][@targetFramework='net8.0']/*[local-name()='dependency'][@id='Microsoft.Extensions.Configuration.EnvironmentVariables']")

      if (-not $environmentVariablesDependency) {
        throw "Package '$($package.Name)' does not declare dependency 'Microsoft.Extensions.Configuration.EnvironmentVariables' for net8.0."
      }

      if ($environmentVariablesDependency.version -ne "8.0.0") {
        throw "Package '$($package.Name)' declares Microsoft.Extensions.Configuration.EnvironmentVariables version '$($environmentVariablesDependency.version)', expected '8.0.0'."
      }

      Write-Host "  Testing package content checks passed."
    }
    else {
      Write-Host "  No framework-specific content checks required for '$id'."
    }
  }
  finally {
    $zip.Dispose()
  }
}
