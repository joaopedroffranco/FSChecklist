#requires -Version 5.1

[CmdletBinding()]
param(
    [string]$Version = 'v1.2.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$executable = Join-Path $repositoryRoot 'FSChecklist.exe'
$simConnect = Join-Path $repositoryRoot 'SimConnect.dll'
$checklists = Join-Path $repositoryRoot 'checklists'
$releaseNotes = Join-Path $repositoryRoot 'RELEASE_NOTES_v1.2.0.md'
$releaseRoot = Join-Path $repositoryRoot 'release'
$packageName = "FSChecklist-$Version-win-x64"
$stagingDirectory = Join-Path $releaseRoot $packageName
$archive = Join-Path $releaseRoot "$packageName.zip"
$checksumFile = "$archive.sha256"

if (-not (Test-Path -LiteralPath $executable)) {
    throw 'FSChecklist.exe not found. Run build.ps1 before packaging.'
}

if (-not (Test-Path -LiteralPath $simConnect)) {
    throw 'SimConnect.dll not found beside the release script.'
}

if (-not (Test-Path -LiteralPath $checklists)) {
    throw 'The checklists directory was not found.'
}

function Get-PeMachineType([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $reader = New-Object IO.BinaryReader($stream)
    try {
        if ($reader.ReadUInt16() -ne 0x5A4D) {
            throw "$Path is not a valid Windows PE file."
        }
        $stream.Position = 0x3C
        $peOffset = $reader.ReadInt32()
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) {
            throw "$Path does not contain a valid PE header."
        }
        return $reader.ReadUInt16()
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

$x64MachineType = 0x8664
if ((Get-PeMachineType $executable) -ne $x64MachineType) {
    throw 'FSChecklist.exe is not an x64 executable.'
}
if ((Get-PeMachineType $simConnect) -ne $x64MachineType) {
    throw 'SimConnect.dll is not an x64 library.'
}

$releaseRootPath = [IO.Path]::GetFullPath($releaseRoot)
$stagingPath = [IO.Path]::GetFullPath($stagingDirectory)
if (-not $stagingPath.StartsWith($releaseRootPath, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The staging directory resolved outside the release directory.'
}

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

Copy-Item -LiteralPath $executable -Destination $stagingDirectory
Copy-Item -LiteralPath $simConnect -Destination $stagingDirectory
Copy-Item -LiteralPath $checklists -Destination $stagingDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'README.md') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE.txt') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'PRIVACY.txt') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'THIRD-PARTY-NOTICES.txt') -Destination $stagingDirectory
Copy-Item -LiteralPath $releaseNotes -Destination $stagingDirectory

if (Test-Path -LiteralPath $archive) {
    Remove-Item -LiteralPath $archive -Force
}

if (Test-Path -LiteralPath $checksumFile) {
    Remove-Item -LiteralPath $checksumFile -Force
}

Compress-Archive -Path $stagingDirectory -DestinationPath $archive `
    -CompressionLevel Optimal

$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" |
    Set-Content -LiteralPath $checksumFile -Encoding ascii

Write-Host "Package: $archive"
Write-Host "SHA-256: $hash"
