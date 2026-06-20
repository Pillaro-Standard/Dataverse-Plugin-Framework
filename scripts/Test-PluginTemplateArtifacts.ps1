[CmdletBinding()]
param(
    [string]$TemplateName = 'Pillaro.Dataverse.PluginTemplate',
    [string]$VsixVersion = '1.0.8',
    [string]$ArtifactsDirectory,
    [string]$Configuration = 'Debug',
    [string]$SmokeProjectName = 'Pill.RemoteSmoke',
    [switch]$SkipBuildSmoke,
    [switch]$SkipDotnetTemplateSmoke
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')

if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) {
    $ArtifactsDirectory = Join-Path $repoRoot 'artifacts\templates'
}

$artifactsRoot = [System.IO.Path]::GetFullPath($ArtifactsDirectory)
$workspaceArtifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$zipPath = Join-Path $artifactsRoot "$TemplateName.zip"
$vsixPath = Join-Path $artifactsRoot "$TemplateName.VisualStudio.vsix"

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found: $Path"
    }
}

function Remove-SafeDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($workspaceArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside artifacts: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
}

function Get-ZipEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $Path))
    $entries = @{}

    foreach ($entry in $zip.Entries) {
        $entries[$entry.FullName.Replace('/', '\')] = $entry
    }

    return @{
        Zip = $zip
        Entries = $entries
    }
}

function Test-ProjectTemplateItems {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$Node,
        [Parameter(Mandatory = $true)]
        [string]$TemplateBasePath,
        [string]$RelativeFolder = '',
        [Parameter(Mandatory = $true)]
        [hashtable]$ZipEntries,
        [System.Collections.Generic.List[string]]$MissingPaths
    )

    foreach ($child in $Node.ChildNodes) {
        if ($child.LocalName -eq 'Folder') {
            $folderName = $child.Attributes['Name'].Value
            $childRelativeFolder = if ([string]::IsNullOrWhiteSpace($RelativeFolder)) {
                $folderName
            }
            else {
                Join-Path $RelativeFolder $folderName
            }

            Test-ProjectTemplateItems -Node $child -TemplateBasePath $TemplateBasePath -RelativeFolder $childRelativeFolder -ZipEntries $ZipEntries -MissingPaths $MissingPaths
        }
        elseif ($child.LocalName -eq 'ProjectItem') {
            $itemPath = $child.InnerText.Trim()
            $sourceRelativePath = if ([string]::IsNullOrWhiteSpace($RelativeFolder)) {
                $itemPath
            }
            else {
                Join-Path $RelativeFolder $itemPath
            }

            $sourcePath = Join-Path $TemplateBasePath $sourceRelativePath
            if (-not $ZipEntries.ContainsKey($sourcePath)) {
                $MissingPaths.Add($sourcePath)
            }
        }
    }
}

function Test-VisualStudioTemplateZip {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Assert-PathExists -Path $Path -Description 'Template ZIP'

    $zipInfo = Get-ZipEntries -Path $Path
    $zip = $zipInfo.Zip
    $entries = $zipInfo.Entries

    try {
        $requiredEntries = @(
            "$TemplateName.vstemplate",
            'Logic\Logic.vstemplate',
            'Logic\Pillaro.Dataverse.PluginTemplate.Logic.csproj',
            'Logic\Plugins\PluginBase.cs',
            'Logic\Plugins\ExamplePlugin.cs',
            'Logic\Tasks\Example\ExampleTask.cs',
            'Plugins\Plugins.vstemplate',
            'Plugins\Pillaro.Dataverse.PluginTemplate.Plugins.csproj',
            'Plugins\PluginAssemblyInfo.cs',
            'Plugins\key.snk',
            'Tests\Tests.vstemplate',
            'Tests\Pillaro.Dataverse.PluginTemplate.Tests.csproj',
            'Tests\TestAutofacModule.cs',
            'Tests\appsettings.json',
            'Tests\Tests\TestBase.cs',
            'Tests\Tests\ConnectionTests.cs'
        )

        $missing = New-Object System.Collections.Generic.List[string]
        foreach ($requiredEntry in $requiredEntries) {
            if (-not $entries.ContainsKey($requiredEntry)) {
                $missing.Add($requiredEntry)
            }
        }

        foreach ($forbidden in @('Entities\', 'ExampleAccount.cs', 'bin\', 'obj\')) {
            $matches = $entries.Keys | Where-Object { $_ -like "*$forbidden*" }
            foreach ($match in $matches) {
                $missing.Add("Forbidden entry found: $match")
            }
        }

        $keyEntry = $entries['Plugins\key.snk']
        if ($keyEntry -and $keyEntry.Length -le 0) {
            $missing.Add('Plugins\key.snk is empty')
        }

        $childTemplates = @(
            'Logic\Logic.vstemplate',
            'Plugins\Plugins.vstemplate',
            'Tests\Tests.vstemplate'
        )

        foreach ($templateEntryName in $childTemplates) {
            $entry = $entries[$templateEntryName]
            if ($null -eq $entry) {
                continue
            }

            $reader = New-Object System.IO.StreamReader($entry.Open())
            try {
                [xml]$xml = $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
            $ns.AddNamespace('vst', 'http://schemas.microsoft.com/developer/vstemplate/2005')
            $basePath = Split-Path $templateEntryName

            $project = $xml.SelectSingleNode('//vst:Project', $ns)
            if ($project -and $project.File) {
                $projectPath = Join-Path $basePath $project.File
                if (-not $entries.ContainsKey($projectPath)) {
                    $missing.Add($projectPath)
                }
            }

            if ($project) {
                Test-ProjectTemplateItems -Node $project -TemplateBasePath $basePath -ZipEntries $entries -MissingPaths $missing
            }
        }

        if ($missing.Count -gt 0) {
            $paths = ($missing | Sort-Object -Unique) -join [Environment]::NewLine
            throw "Template ZIP validation failed:$([Environment]::NewLine)$paths"
        }

        Write-Host "Template ZIP validation passed: $Path"
    }
    finally {
        $zip.Dispose()
    }
}

function Test-VisualStudioVsix {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Assert-PathExists -Path $Path -Description 'VSIX package'

    $zipInfo = Get-ZipEntries -Path $Path
    $zip = $zipInfo.Zip
    $entries = $zipInfo.Entries

    try {
        $requiredEntries = @(
            'extension.vsixmanifest',
            '[Content_Types].xml',
            'manifest.json',
            'catalog.json',
            'Assets\PillaroLogo128.png',
            'ProjectTemplates\templateManifest0.noloc.vstman',
            "ProjectTemplates\$TemplateName\$TemplateName.vstemplate",
            "ProjectTemplates\$TemplateName\Logic\Logic.vstemplate",
            "ProjectTemplates\$TemplateName\Plugins\Plugins.vstemplate",
            "ProjectTemplates\$TemplateName\Tests\Tests.vstemplate"
        )

        $missing = $requiredEntries | Where-Object { -not $entries.ContainsKey($_) }
        if ($missing) {
            throw "VSIX package is missing required files: $($missing -join ', ')"
        }

        $manifestEntry = $entries['extension.vsixmanifest']
        $reader = New-Object System.IO.StreamReader($manifestEntry.Open())
        try {
            [xml]$manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $ns = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
        $ns.AddNamespace('vsx', 'http://schemas.microsoft.com/developer/vsx-schema/2011')

        $identity = $manifest.SelectSingleNode('//vsx:Identity', $ns)
        $icon = $manifest.SelectSingleNode('//vsx:Icon', $ns)
        $packageManifest = $manifest.SelectSingleNode('/vsx:PackageManifest', $ns)

        if (-not $identity) {
            throw 'VSIX manifest does not contain an Identity element.'
        }

        if (-not $packageManifest -or $packageManifest.Version -ne '2.0.0') {
            throw "VSIX PackageManifest version is '$($packageManifest.Version)', expected '2.0.0'."
        }

        if ($identity.Version -ne $VsixVersion) {
            throw "VSIX manifest version is '$($identity.Version)', expected '$VsixVersion'."
        }

        if (-not $icon -or $icon.InnerText -ne 'Assets\PillaroLogo128.png') {
            throw 'VSIX manifest icon must point to Assets\PillaroLogo128.png.'
        }

        $contentTypesEntry = $entries['[Content_Types].xml']
        $reader = New-Object System.IO.StreamReader($contentTypesEntry.Open())
        try {
            $contentTypes = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        if ($contentTypes -notmatch 'Extension="png"') {
            throw 'VSIX [Content_Types].xml does not declare PNG content type.'
        }

        Write-Host "VSIX validation passed: $Path"
    }
    finally {
        $zip.Dispose()
    }
}

function Get-MSBuildPath {
    $vswhereCandidates = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    )

    foreach ($vswhere in $vswhereCandidates) {
        if (Test-Path -LiteralPath $vswhere) {
            $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
            if (-not [string]::IsNullOrWhiteSpace($msbuild) -and (Test-Path -LiteralPath $msbuild)) {
                return $msbuild
            }
        }
    }

    $fallbacks = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($fallback in $fallbacks) {
        if (Test-Path -LiteralPath $fallback) {
            return $fallback
        }
    }

    throw 'MSBuild.exe was not found. Install Visual Studio Build Tools or run from a Visual Studio environment.'
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [string]$WorkingDirectory = $repoRoot
    )

    Write-Host "> $FilePath $($Arguments -join ' ')"

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments

        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Replace-TemplateTokensInFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$SafeProjectName,
        [Parameter(Mandatory = $true)]
        [string]$RootProjectName
    )

    $extension = [System.IO.Path]::GetExtension($Path)
    if ($extension -notin @('.cs', '.csproj', '.json', '.config', '.xml')) {
        return
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $content = $content.Replace('$safeprojectname$', $SafeProjectName)
    $content = $content.Replace('$ext_safeprojectname$', $RootProjectName)
    $content = $content.Replace('$projectname$', $SafeProjectName)
    $content = $content.Replace('$ext_projectname$', $RootProjectName)
    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
}

function Copy-VisualStudioTemplateItems {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$Node,
        [Parameter(Mandatory = $true)]
        [string]$SourceFolder,
        [Parameter(Mandatory = $true)]
        [string]$DestinationFolder,
        [Parameter(Mandatory = $true)]
        [string]$SafeProjectName,
        [Parameter(Mandatory = $true)]
        [string]$RootProjectName
    )

    foreach ($child in $Node.ChildNodes) {
        if ($child.LocalName -eq 'Folder') {
            $sourceChildFolder = Join-Path $SourceFolder $child.Attributes['Name'].Value
            $targetFolderName = if ($child.Attributes['TargetFolderName']) {
                $child.Attributes['TargetFolderName'].Value
            }
            else {
                $child.Attributes['Name'].Value
            }
            $destinationChildFolder = Join-Path $DestinationFolder $targetFolderName

            New-Item -ItemType Directory -Path $destinationChildFolder -Force | Out-Null
            Copy-VisualStudioTemplateItems -Node $child -SourceFolder $sourceChildFolder -DestinationFolder $destinationChildFolder -SafeProjectName $SafeProjectName -RootProjectName $RootProjectName
        }
        elseif ($child.LocalName -eq 'ProjectItem') {
            $sourcePath = Join-Path $SourceFolder $child.InnerText.Trim()
            $targetFileName = if ($child.Attributes['TargetFileName']) {
                $child.Attributes['TargetFileName'].Value
            }
            else {
                [System.IO.Path]::GetFileName($child.InnerText.Trim())
            }
            $destinationPath = Join-Path $DestinationFolder $targetFileName

            Assert-PathExists -Path $sourcePath -Description 'Visual Studio template source item'
            New-Item -ItemType Directory -Path (Split-Path -Parent $destinationPath) -Force | Out-Null
            Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force

            if ($child.Attributes['ReplaceParameters'] -and $child.Attributes['ReplaceParameters'].Value -eq 'true') {
                Replace-TemplateTokensInFile -Path $destinationPath -SafeProjectName $SafeProjectName -RootProjectName $RootProjectName
            }
        }
    }
}

function Copy-VisualStudioSubProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExtractedRoot,
        [Parameter(Mandatory = $true)]
        [string]$TemplateFolderName,
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$RootProjectName
    )

    $sourceFolder = Join-Path $ExtractedRoot $TemplateFolderName
    $safeProjectName = "$RootProjectName.$TemplateFolderName"
    $destinationFolder = Join-Path $GeneratedRoot $safeProjectName
    $templatePath = Join-Path $sourceFolder "$TemplateFolderName.vstemplate"

    [xml]$template = Get-Content -LiteralPath $templatePath -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($template.NameTable)
    $ns.AddNamespace('vst', 'http://schemas.microsoft.com/developer/vstemplate/2005')
    $project = $template.SelectSingleNode('//vst:Project', $ns)

    if (-not $project -or -not $project.File) {
        throw "Project node was not found in $templatePath."
    }

    New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
    $sourceProjectPath = Join-Path $sourceFolder $project.File
    $targetProjectPath = Join-Path $destinationFolder "$safeProjectName.csproj"
    Copy-Item -LiteralPath $sourceProjectPath -Destination $targetProjectPath -Force
    Replace-TemplateTokensInFile -Path $targetProjectPath -SafeProjectName $safeProjectName -RootProjectName $RootProjectName

    Copy-VisualStudioTemplateItems -Node $project -SourceFolder $sourceFolder -DestinationFolder $destinationFolder -SafeProjectName $safeProjectName -RootProjectName $RootProjectName

    return $targetProjectPath
}

function Invoke-VisualStudioTemplateSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $smokeRoot = Join-Path $workspaceArtifactsRoot 'template-smoke-vszip'
    $extractRoot = Join-Path $smokeRoot 'extracted'
    $generatedRoot = Join-Path $smokeRoot 'generated'

    Remove-SafeDirectory -Path $smokeRoot
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null

    Expand-Archive -LiteralPath $Path -DestinationPath $extractRoot -Force

    $projectFiles = @()
    $projectFiles += Copy-VisualStudioSubProject -ExtractedRoot $extractRoot -TemplateFolderName 'Logic' -GeneratedRoot $generatedRoot -RootProjectName $SmokeProjectName
    $projectFiles += Copy-VisualStudioSubProject -ExtractedRoot $extractRoot -TemplateFolderName 'Plugins' -GeneratedRoot $generatedRoot -RootProjectName $SmokeProjectName
    $projectFiles += Copy-VisualStudioSubProject -ExtractedRoot $extractRoot -TemplateFolderName 'Tests' -GeneratedRoot $generatedRoot -RootProjectName $SmokeProjectName

    foreach ($requiredPath in @(
        (Join-Path $generatedRoot "$SmokeProjectName.Logic\Plugins\ExamplePlugin.cs"),
        (Join-Path $generatedRoot "$SmokeProjectName.Logic\Tasks\Example\ExampleTask.cs"),
        (Join-Path $generatedRoot "$SmokeProjectName.Plugins\key.snk"),
        (Join-Path $generatedRoot "$SmokeProjectName.Tests\Tests\ConnectionTests.cs"),
        (Join-Path $generatedRoot "$SmokeProjectName.Tests\Data\CleanupHandlers"),
        (Join-Path $generatedRoot "$SmokeProjectName.Tests\Data\Repositories")
    )) {
        Assert-PathExists -Path $requiredPath -Description 'Generated template file'
    }

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @('new', 'sln', '--name', $SmokeProjectName) -WorkingDirectory $generatedRoot

    $solution = Get-ChildItem -LiteralPath $generatedRoot -Filter "$SmokeProjectName.sln" -File | Select-Object -First 1
    if (-not $solution) {
        $solution = Get-ChildItem -LiteralPath $generatedRoot -Filter "$SmokeProjectName.slnx" -File | Select-Object -First 1
    }

    if (-not $solution) {
        throw "dotnet new sln did not create a solution under $generatedRoot."
    }

    $slnPath = $solution.FullName
    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments (@('sln', $slnPath, 'add') + $projectFiles) -WorkingDirectory $generatedRoot

    $msbuild = Get-MSBuildPath
    Invoke-CheckedCommand -FilePath $msbuild -Arguments @(
        $slnPath,
        '/t:Restore,Build',
        "/p:Configuration=$Configuration",
        '/p:UseSharedCompilation=false',
        '/m:1'
    ) -WorkingDirectory $generatedRoot

    Write-Host "Visual Studio template smoke build passed: $slnPath"
}

function Invoke-DotnetTemplateSmoke {
    # The dotnet new template package is validated by Test-DotNetTemplateArtifacts.ps1.
    # This script focuses on the Visual Studio VSIX outputs.
    Write-Host 'Dotnet template smoke intentionally skipped in the VSIX validator; use Test-DotNetTemplateArtifacts.ps1 for NuGet template validation.'
}

Test-VisualStudioTemplateZip -Path $zipPath
Test-VisualStudioVsix -Path $vsixPath

if (-not $SkipBuildSmoke) {
    Invoke-VisualStudioTemplateSmoke -Path $zipPath
}

if (-not $SkipDotnetTemplateSmoke) {
    Invoke-DotnetTemplateSmoke
}

Write-Host 'Plugin template artifact validation passed.'
