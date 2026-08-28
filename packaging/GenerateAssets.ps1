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

function Get-IcoBitmap {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 22) {
        throw "El archivo ICO no es válido: $Path"
    }

    $count = [System.BitConverter]::ToUInt16($bytes, 4)
    if ($count -lt 1) {
        throw "El archivo ICO no contiene imágenes: $Path"
    }

    $bestOffset = 0
    $bestLength = 0
    $bestArea = -1

    for ($i = 0; $i -lt $count; $i++) {
        $entry = 6 + ($i * 16)
        if (($entry + 16) -gt $bytes.Length) {
            throw "La tabla del archivo ICO está dañada: $Path"
        }

        $width = [int]$bytes[$entry]
        $height = [int]$bytes[$entry + 1]
        if ($width -eq 0) { $width = 256 }
        if ($height -eq 0) { $height = 256 }

        $length = [System.BitConverter]::ToUInt32($bytes, $entry + 8)
        $offset = [System.BitConverter]::ToUInt32($bytes, $entry + 12)
        $area = $width * $height

        if (($offset + $length) -le $bytes.Length -and $area -gt $bestArea) {
            $bestArea = $area
            $bestOffset = [int]$offset
            $bestLength = [int]$length
        }
    }

    if ($bestLength -le 0) {
        throw "No se pudo extraer una imagen válida del ICO: $Path"
    }

    $imageBytes = New-Object byte[] $bestLength
    [System.Array]::Copy($bytes, $bestOffset, $imageBytes, 0, $bestLength)

    $isPng = $bestLength -ge 8 -and
        $imageBytes[0] -eq 0x89 -and
        $imageBytes[1] -eq 0x50 -and
        $imageBytes[2] -eq 0x4E -and
        $imageBytes[3] -eq 0x47 -and
        $imageBytes[4] -eq 0x0D -and
        $imageBytes[5] -eq 0x0A -and
        $imageBytes[6] -eq 0x1A -and
        $imageBytes[7] -eq 0x0A

    if ($isPng) {
        $stream = New-Object System.IO.MemoryStream(,$imageBytes)
        try {
            $image = [System.Drawing.Image]::FromStream($stream)
            try {
                return New-Object System.Drawing.Bitmap($image)
            }
            finally {
                $image.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }

    $icon = New-Object System.Drawing.Icon($Path)
    try {
        return $icon.ToBitmap()
    }
    finally {
        $icon.Dispose()
    }
}

$sourceBitmap = Get-IcoBitmap $source

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
}

Write-Host "Assets MSIX generados en: $output"
