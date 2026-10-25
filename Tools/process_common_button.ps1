param(
    [string]$SourceImage = "bg_common_button_4.jpg",
    [string]$OutputName = "bg_common_button_4.png",
    [int]$TargetWidth = 896
)
Add-Type -AssemblyName System.Drawing
$assetsRoot = "C:\Users\zll\.cursor\projects\d-Unity-Project-TimeChip-TimeChip\assets"
$src = Join-Path $assetsRoot $SourceImage
$dst = Join-Path (Split-Path $PSScriptRoot -Parent) "Assets\Art\Common\SpritesBg\$OutputName"
$bmp = [System.Drawing.Bitmap]::FromFile($src)
$w = $bmp.Width
$h = $bmp.Height
$out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $c = $bmp.GetPixel($x, $y)
        $lum = ($c.R + $c.G + $c.B) / 3.0
        if ($lum -lt 24) {
            $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        }
        elseif ($c.R -gt 242 -and $c.G -gt 242 -and $c.B -gt 242) {
            $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        }
        else {
            $out.SetPixel($x, $y, $c)
        }
    }
}
$bmp.Dispose()

$rowThreshold = [Math]::Max(8, [int]($w * 0.02))
$minX = $w; $minY = $h; $maxX = 0; $maxY = 0
for ($y = 0; $y -lt $h; $y++) {
    $count = 0
    for ($x = 0; $x -lt $w; $x++) {
        if ($out.GetPixel($x, $y).A -gt 8) { $count++ }
    }
    if ($count -lt $rowThreshold) { continue }
    for ($x = 0; $x -lt $w; $x++) {
        if ($out.GetPixel($x, $y).A -gt 8) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
        }
    }
    if ($y -lt $minY) { $minY = $y }
    if ($y -gt $maxY) { $maxY = $y }
}
$pad = 2
$minX = [Math]::Max(0, $minX - $pad)
$minY = [Math]::Max(0, $minY - $pad)
$maxX = [Math]::Min($w - 1, $maxX + $pad)
$maxY = [Math]::Min($h - 1, $maxY + $pad)
$cw = $maxX - $minX + 1
$ch = $maxY - $minY + 1
$cropped = New-Object System.Drawing.Bitmap $cw, $ch, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($cropped)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.DrawImage($out, 0, 0, (New-Object System.Drawing.Rectangle $minX, $minY, $cw, $ch), [System.Drawing.GraphicsUnit]::Pixel)
$graphics.Dispose()
$out.Dispose()

$targetH = [int][Math]::Round($ch * ($TargetWidth / $cw))
$scaled = New-Object System.Drawing.Bitmap $TargetWidth, $targetH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics2 = [System.Drawing.Graphics]::FromImage($scaled)
$graphics2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics2.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics2.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics2.DrawImage($cropped, 0, 0, $TargetWidth, $targetH)
$graphics2.Dispose()
$cropped.Dispose()
$scaled.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
$scaled.Dispose()
Write-Host "Saved $dst ($TargetWidth x $targetH)"
