#requires -Version 5.1

$ErrorActionPreference = 'Stop'

$localDotnet = Join-Path $PSScriptRoot '.dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) {
    $localDotnet
} else {
    (Get-Command dotnet -ErrorAction Stop).Source
}
$project = Join-Path $PSScriptRoot 'src\FSChecklist.csproj'
$outputDirectory = Join-Path $PSScriptRoot 'dist'
$output = Join-Path $outputDirectory 'FSChecklist.exe'
$publishDirectory = Join-Path $PSScriptRoot '.build-output'
$checklistOutput = Join-Path $outputDirectory 'checklists'

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $checklistOutput -Force | Out-Null

$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.nuget\packages'
$env:APPDATA = Join-Path $PSScriptRoot '.appdata'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
New-Item -ItemType Directory -Path (Join-Path $env:DOTNET_CLI_HOME '.dotnet\tools') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $env:APPDATA 'NuGet') -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') `
    (Join-Path $env:APPDATA 'NuGet\NuGet.Config') -Force

& $dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --output $publishDirectory `
    --configfile (Join-Path $PSScriptRoot 'NuGet.Config') `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "Falha na compilacao: dotnet retornou $LASTEXITCODE"
}

$publishedExecutable = Join-Path $publishDirectory 'FSChecklist.exe'
try {
    Copy-Item $publishedExecutable $output -Force -ErrorAction Stop
} catch {
    throw 'Nao foi possivel sobrescrever dist\FSChecklist.exe. Feche o aplicativo e execute o build novamente.'
}

$signingCertificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        $_.Subject -eq 'CN=FSChecklist Local' -and
        $_.HasPrivateKey -and
        $_.NotAfter -gt (Get-Date)
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $signingCertificate) {
    throw 'Certificado de assinatura "CN=FSChecklist Local" nao encontrado em Cert:\CurrentUser\My.'
}

$signTool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
    -Recurse `
    -Filter signtool.exe `
    -ErrorAction SilentlyContinue |
    Where-Object FullName -Like '*\x64\signtool.exe' |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $signTool) {
    throw 'SignTool nao encontrado. Instale o Windows SDK para assinar o executavel.'
}

& $signTool.FullName sign `
    /fd SHA256 `
    /sha1 $signingCertificate.Thumbprint `
    /s My `
    $output

if ($LASTEXITCODE -ne 0) {
    throw "Falha ao assinar o executavel: SignTool retornou $LASTEXITCODE"
}

Copy-Item (Join-Path $PSScriptRoot 'checklists\*.json') $checklistOutput -Force
Write-Host "Build concluido: $output"
