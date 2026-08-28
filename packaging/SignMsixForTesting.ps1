param(
    [string]$Version = "1.0.0.0",
    [string]$Publisher = "CN=EFE2FF1A-8478-41FD-A0B3-9DBE8DFE43B1"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root "msix-output"
$sourcePackage = Join-Path $output "WinFlow_$Version`_x64.msix"
$testPackage = Join-Path $output "WinFlow_$Version`_x64-test.msix"
$certificatePath = Join-Path $output "WinFlow-Test.cer"
$friendlyName = "WinFlow MSIX Test Certificate"

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

if (-not (Test-Path $sourcePackage)) {
    throw "No se encontró el MSIX: $sourcePackage. Ejecuta BuildMsix.ps1 primero."
}

New-Item -ItemType Directory -Force -Path $output | Out-Null

$certificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        $_.Subject -eq $Publisher -and
        $_.FriendlyName -eq $friendlyName -and
        $_.NotAfter -gt (Get-Date).AddDays(1)
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $certificate) {
    Write-Host "Creando certificado local de prueba..."
    $certificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $Publisher `
        -KeyUsage DigitalSignature `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @(
            "2.5.29.37={text}1.3.6.1.5.5.7.3.3",
            "2.5.29.19={text}"
        ) `
        -FriendlyName $friendlyName
}

Export-Certificate -Cert $certificate -FilePath $certificatePath -Force | Out-Null

Write-Host "Confiando el certificado de prueba en este PC..."
$escapedCertificatePath = $certificatePath.Replace("'", "''")
$trustCommand = "Import-Certificate -FilePath '$escapedCertificatePath' -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null"
$encodedTrustCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($trustCommand))
$trustProcess = Start-Process powershell.exe `
    -Verb RunAs `
    -Wait `
    -PassThru `
    -ArgumentList "-NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedTrustCommand"

if ($trustProcess.ExitCode -ne 0) {
    throw "No se pudo confiar el certificado de prueba. Acepta el aviso de administrador e inténtalo de nuevo."
}

Copy-Item $sourcePackage $testPackage -Force

$signTool = Find-WindowsSdkTool "SignTool.exe"
Write-Host "Firmando copia local de prueba..."
& $signTool sign `
    /fd SHA256 `
    /s My `
    /sha1 $certificate.Thumbprint `
    $testPackage

if ($LASTEXITCODE -ne 0) {
    throw "SignTool falló."
}

Write-Host ""
Write-Host "MSIX de prueba firmado correctamente:"
Write-Host $testPackage
Write-Host ""
Write-Host "Certificado público de prueba:"
Write-Host $certificatePath
Write-Host ""
Write-Host "Este certificado es solo para pruebas locales. El MSIX original para Partner Center no se modifica."
