#requires -Version 5.1

$ErrorActionPreference = 'Stop'

$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$speechAssembly = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Speech\v4.0_4.0.0.0__31bf3856ad364e35\System.Speech.dll'
$source = Join-Path $PSScriptRoot 'src\FSChecklist\Program.cs'
$outputDirectory = Join-Path $PSScriptRoot 'dist'
$output = Join-Path $outputDirectory 'FSChecklist.exe'
$checklistOutput = Join-Path $outputDirectory 'checklists'

if (-not (Test-Path $compiler)) {
    throw "Compilador do Windows nao encontrado: $compiler"
}
if (-not (Test-Path $speechAssembly)) {
    throw "Biblioteca de voz do Windows nao encontrada: $speechAssembly"
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $checklistOutput -Force | Out-Null

& $compiler /nologo /target:winexe /platform:x64 /optimize+ `
    /out:$output `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    /reference:$speechAssembly `
    $source

if ($LASTEXITCODE -ne 0) {
    throw "Falha na compilacao: csc retornou $LASTEXITCODE"
}

Copy-Item (Join-Path $PSScriptRoot 'checklists\*.json') $checklistOutput -Force
Write-Host "Build concluido: $output"
