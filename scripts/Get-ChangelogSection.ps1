[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$ChangelogPath,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ChangelogPath)) {
    $ChangelogPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'CHANGELOG.md'
}

if (-not (Test-Path -LiteralPath $ChangelogPath)) {
    throw "Changelog was not found: $ChangelogPath"
}

$lines = [System.IO.File]::ReadAllLines($ChangelogPath, [System.Text.Encoding]::UTF8)

# Section headings are '## <version>'. Anything at the same level ends the section.
# Matched exactly so that '## 1.2.0' is not satisfied by '## 1.2.0-rc'.
$heading = "## $Version"
$startIndex = -1

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i].Trim() -eq $heading) {
        $startIndex = $i
        break
    }
}

if ($startIndex -lt 0) {
    throw "Changelog has no '$heading' section. Add the release section before tagging."
}

$endIndex = $lines.Length

for ($i = $startIndex + 1; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^##\s') {
        $endIndex = $i
        break
    }
}

$body = $lines[($startIndex + 1)..($endIndex - 1)] -join "`n"
$body = $body.Trim()

if ([string]::IsNullOrWhiteSpace($body)) {
    throw "The '$heading' section in $ChangelogPath is empty."
}

if ($OutputPath) {
    $encoding = New-Object System.Text.UTF8Encoding -ArgumentList $false
    [System.IO.File]::WriteAllText($OutputPath, $body, $encoding)
    Write-Host "Wrote $($body.Split("`n").Length) line(s) of release notes to $OutputPath"
}
else {
    Write-Output $body
}
