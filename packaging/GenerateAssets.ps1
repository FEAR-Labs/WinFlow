param(
    [string]$SourceIcon = "assets\icons\WinFlow-Icon.ico",
    [string]$OutputDirectory = "packaging\Assets"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root $SourceIcon
$output = Join-Path $root $OutputDirectory

if (-not (Test-Path $source)) {
    throw "No se encontró el icono fuente: $source"
}

New-Item -ItemType Directory -Force -Path $output | Out-Null

$icon = New-Object System.Drawing.Icon($source)
$sourceBitmap = $icon.ToBitmap()

function Save-SquareAsset {
    param(
        [string]$Name,
        [int]$Size
    )

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($sourceBitmap, 0, 0, $Size, $Size)
        $bitmap.Save((Join-Path $output $Name), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Save-WideAsset {
    param(
        [string]$Name,
        [int]$Width,
        [int]$Height,
        [int]$IconSize
    )

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $x = [int](($Width - $IconSize) / 2)
        $y = [int](($Height - $IconSize) / 2)
        $graphics.DrawImage($sourceBitmap, $x, $y, $IconSize, $IconSize)
        $bitmap.Save((Join-Path $output $Name), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

try {
    Save-SquareAsset "StoreLogo.png" 50
    Save-SquareAsset "Square44x44Logo.png" 44
    Save-SquareAsset "Square44x44Logo.scale-200.png" 88
    Save-SquareAsset "Square44x44Logo.scale-400.png" 176
    Save-SquareAsset "Square44x44Logo.targetsize-16.png" 16
    Save-SquareAsset "Square44x44Logo.targetsize-24.png" 24
    Save-SquareAsset "Square44x44Logo.targetsize-32.png" 32
    Save-SquareAsset "Square44x44Logo.targetsize-48.png" 48
    Save-SquareAsset "Square44x44Logo.targetsize-256.png" 256

    Save-SquareAsset "Square150x150Logo.png" 150
    Save-SquareAsset "Square150x150Logo.scale-200.png" 300
    Save-SquareAsset "Square150x150Logo.scale-400.png" 600
    Save-SquareAsset "Square310x310Logo.png" 310
    Save-WideAsset "Wide310x150Logo.png" 310 150 120
}
finally {
    $sourceBitmap.Dispose()
    $icon.Dispose()
}

Write-Host "Assets MSIX generados en: $output"
