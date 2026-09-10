[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyVersion,

    [Parameter(Mandatory = $true)]
    [string]$PackageVersion,

    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot,

    [Parameter(Mandatory = $false)]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Lifted from the "Verify package DLL metadata" step of the Azure DevOps packages
# pipeline, with its pipeline macros turned into parameters. Extracted mechanically
# rather than retyped, so the checks are unchanged.

$expectedAssemblyVersion = [version]$AssemblyVersion
$packageVersion = $PackageVersion
$dlls = @(
  @{ Path = "$RepositoryRoot\src\Pillaro.Dataverse.PluginFramework\bin\$Configuration\net462\Pillaro.Dataverse.PluginFramework.dll"; Optional = $false },
  @{ Path = "$RepositoryRoot\src\Pillaro.Dataverse.PluginFramework\bin\$Configuration\net8.0\Pillaro.Dataverse.PluginFramework.dll"; Optional = $false },
  @{ Path = "$RepositoryRoot\src\Pillaro.Dataverse.PluginFramework.Testing\bin\$Configuration\net8.0\Pillaro.Dataverse.PluginFramework.Testing.dll"; Optional = $false },
  @{ Path = "$RepositoryRoot\tools\Pillaro.Dataverse.PluginFramework.Cli\bin\$Configuration\net8.0\publish\Pillaro.Dataverse.PluginFramework.dll"; Optional = $true },
  @{ Path = "$RepositoryRoot\tools\Pillaro.Dataverse.PluginFramework.Cli\bin\$Configuration\net8.0\publish\pillaro-dv.dll"; Optional = $true }
)

function Test-PortablePdb {
  param(
    [string] $Path
  )

  $stream = [System.IO.File]::OpenRead($Path)
  try {
    if ($stream.Length -lt 4) {
      return $false
    }

    $signature = New-Object byte[] 4
    [void]$stream.Read($signature, 0, 4)
    return $signature[0] -eq 0x42 -and $signature[1] -eq 0x53 -and $signature[2] -eq 0x4A -and $signature[3] -eq 0x42
  }
  finally {
    $stream.Dispose()
  }
}

foreach ($dll in $dlls) {
  $path = $dll.Path
  $pdbPath = [System.IO.Path]::ChangeExtension($path, ".pdb")

  if (-not (Test-Path $path)) {
    if ($dll.Optional) {
      Write-Host "Skipping optional DLL metadata check because file was not found: $path"
      continue
    }

    throw "Required DLL was not found for metadata verification: $path"
  }

  $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($path).Version
  $fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($path)

  Write-Host "DLL metadata: $path"
  Write-Host "  AssemblyName.Version: $assemblyVersion"
  Write-Host "  FileVersion: $($fileInfo.FileVersion)"
  Write-Host "  ProductVersion: $($fileInfo.ProductVersion)"
  Write-Host "  PDB: $pdbPath"

  if ($assemblyVersion -ne $expectedAssemblyVersion) {
    throw "AssemblyName.Version for '$path' was '$assemblyVersion', expected '$expectedAssemblyVersion'."
  }

  if ([string]::IsNullOrWhiteSpace($fileInfo.ProductVersion) -or $fileInfo.ProductVersion -notlike "*$packageVersion*") {
    throw "ProductVersion/InformationalVersion for '$path' was '$($fileInfo.ProductVersion)', expected it to contain '$packageVersion'."
  }

  if (-not (Test-Path $pdbPath)) {
    throw "PDB for packaged DLL '$path' was not found at '$pdbPath'."
  }

  if (-not (Test-PortablePdb -Path $pdbPath)) {
    throw "PDB for packaged DLL '$path' is not a portable PDB: $pdbPath"
  }
}
