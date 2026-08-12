#requires -Version 5.1

[CmdletBinding()]
param(
    [string]$Version = '2.1.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = $PSScriptRoot
$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
$compiler = $compilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if (-not $compiler) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $compiler = $command.Source }
}

if (-not $compiler) {
    throw 'Inno Setup 6 nao encontrado. Instale-o e execute este script novamente.'
}

$requiredPaths = @(
    'FSChecklist.exe',
    'SimConnect.dll',
    'checklists',
    'assets\fschecklist.ico'
)
foreach ($requiredPath in $requiredPaths) {
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $requiredPath))) {
        throw "Arquivo necessario nao encontrado: $requiredPath"
    }
}

$script = Join-Path $repositoryRoot 'packaging\FSChecklist.iss'
& $compiler "/DAppVersion=$Version" "/DSourceRoot=$repositoryRoot" $script
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao gerar instalador: Inno Setup retornou $LASTEXITCODE"
}

$installer = Join-Path $repositoryRoot "release\FSChecklist-Setup-$Version-win-x64.exe"

$signingCertificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        $_.Subject -eq 'CN=FSChecklist Local' -and
        $_.HasPrivateKey -and
        $_.NotAfter -gt (Get-Date)
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1
if (-not $signingCertificate) {
    throw 'Certificado de assinatura "CN=FSChecklist Local" nao encontrado.'
}

$signTool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
    -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Where-Object FullName -Like '*\x64\signtool.exe' |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not $signTool) {
    throw 'SignTool nao encontrado. Instale o Windows SDK.'
}

& $signTool.FullName sign /fd SHA256 /sha1 $signingCertificate.Thumbprint `
    /s My $installer
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao assinar o instalador: SignTool retornou $LASTEXITCODE"
}

$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumFile = "$installer.sha256"
Set-Content -LiteralPath $checksumFile -Encoding ascii `
    -Value "$hash  $([IO.Path]::GetFileName($installer))"

Write-Host "Instalador: $installer"
Write-Host "SHA-256: $hash"
