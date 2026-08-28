param(
    [string]$IdentityName = "FEAR-Labs.WinFlow",
    [string]$Publisher = "CN=EFE2FF1A-8478-41FD-A0B3-9DBE8DFE43B1",
    [string]$Version = "1.0.0.0",
    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$staging = Join-Path $root "msix-staging"
$output = Join-Path $root "msix-output"
$publish = Join-Path $staging "publish"
$assets = Join-Path $root "packaging\Assets"
$template = Join-Path $root "packaging\Package.appxmanifest.template"
$manifest = Join-Path $staging "AppxManifest.xml"
$package = Join-Path $output "WinFlow_$Version`_x64.msix"

function Find-WindowsSdkTool {
    param([string]$ToolName)

    $kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    if (-not (Test-Path $kitsRoot)) {
        throw "No se encontró Windows SDK. Instala Windows 10/11 SDK."
    }

    $candidate = Get-ChildItem $kitsRoot -Directory |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\$ToolName" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1

    if (-not $candidate) {
        throw "No se encontró $ToolName en Windows SDK."
    }

    return $candidate
}

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "La versión MSIX debe tener cuatro números, por ejemplo 1.0.0.0."
}

Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $output -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publish, $output | Out-Null

& (Join-Path $PSScriptRoot "GenerateAssets.ps1")

Write-Host "Publicando WinFlow para MSIX..."
dotnet publish (Join-Path $root "WinFlow.csproj") `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publish

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falló."
}

Copy-Item (Join-Path $publish "*") $staging -Recurse -Force
Remove-Item $publish -Recurse -Force
Copy-Item $assets (Join-Path $staging "Assets") -Recurse -Force

$manifestContent = Get-Content $template -Raw
$manifestContent = $manifestContent.Replace('Name="FEAR-Labs.WinFlow"', ('Name="' + $IdentityName + '"'))
$manifestContent = $manifestContent.Replace('Publisher="CN=EFE2FF1A-8478-41FD-A0B3-9DBE8DFE43B1"', ('Publisher="' + $Publisher + '"'))
$manifestContent = $manifestContent.Replace('Version="1.0.0.0"', ('Version="' + $Version + '"'))
Set-Content -Path $manifest -Value $manifestContent -Encoding utf8

$makeAppx = Find-WindowsSdkTool "MakeAppx.exe"
Write-Host "Creando paquete MSIX..."
& $makeAppx pack /d $staging /p $package /o

if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx falló."
}

Write-Host ""
Write-Host "MSIX creado correctamente:"
Write-Host $package
Write-Host ""
Write-Host "Para Microsoft Store no necesitas firmarlo con un certificado CA propio; Partner Center lo firma al publicarlo."
