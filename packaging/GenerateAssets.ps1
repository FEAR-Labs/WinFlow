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
Get-ChildItem -Path $output -Filter "*.png" -ErrorAction SilentlyContinue | Remove-Item -Force

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

function Save-ScaledSquareAssets {
    param(
        [string]$BaseName,
        [int]$BaseSize
    )

    $scales = @(
        @{ Scale = 100; Factor = 1.0 },
        @{ Scale = 125; Factor = 1.25 },
        @{ Scale = 150; Factor = 1.5 },
        @{ Scale = 200; Factor = 2.0 },
        @{ Scale = 400; Factor = 4.0 }
    )

    foreach ($item in $scales) {
        $size = [int][Math]::Ceiling($BaseSize * $item.Factor)
        Save-SquareAsset "$BaseName.scale-$($item.Scale).png" $size
    }
}

function Save-ScaledWideAssets {
    param(
        [string]$BaseName,
        [int]$BaseWidth,
        [int]$BaseHeight,
        [int]$BaseIconSize
    )

    $scales = @(
        @{ Scale = 100; Factor = 1.0 },
        @{ Scale = 125; Factor = 1.25 },
        @{ Scale = 150; Factor = 1.5 },
        @{ Scale = 200; Factor = 2.0 },
        @{ Scale = 400; Factor = 4.0 }
    )

    foreach ($item in $scales) {
        $width = [int][Math]::Ceiling($BaseWidth * $item.Factor)
        $height = [int][Math]::Ceiling($BaseHeight * $item.Factor)
        $iconSize = [int][Math]::Ceiling($BaseIconSize * $item.Factor)
        Save-WideAsset "$BaseName.scale-$($item.Scale).png" $width $height $iconSize
    }
}

try {
    # Package/Store logo. Microsoft Store requires the scale variants.
    Save-ScaledSquareAssets "StoreLogo" 50

    # App-list icon and common target-size variants used by Windows shell surfaces.
    Save-ScaledSquareAssets "Square44x44Logo" 44
    foreach ($size in @(16, 24, 32, 48, 256)) {
        Save-SquareAsset "Square44x44Logo.targetsize-$size.png" $size
        Save-SquareAsset "Square44x44Logo.targetsize-$size`_altform-unplated.png" $size
    }

    # Tile assets. Every declared tile is generated from WinFlow's own icon so
    # certification never falls back to a generic/default product image.
    Save-ScaledSquareAssets "Square150x150Logo" 150
    Save-ScaledSquareAssets "Square310x310Logo" 310
    Save-ScaledWideAssets "Wide310x150Logo" 310 150 120
}
finally {
    $sourceBitmap.Dispose()
}

Write-Host "Assets MSIX generados en: $output"
