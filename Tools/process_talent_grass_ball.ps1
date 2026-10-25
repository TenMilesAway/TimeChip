param(
    [string]$SourceImage = "talent_node_grass_ball.jpg",
    [string]$OutputName = "talent_node_grass_ball.png",
    [double]$InnerRadiusRatio = 0.30
)
Add-Type -AssemblyName System.Drawing
$assetsRoot = "C:\Users\zll\.cursor\projects\d-Unity-Project-TimeChip-TimeChip\assets"
$src = Join-Path $assetsRoot $SourceImage
$dst = Join-Path (Split-Path $PSScriptRoot -Parent) "Assets\Art\Grow\SpritesBig\$OutputName"
$bmp = [System.Drawing.Bitmap]::FromFile($src)
$w = $bmp.Width
$h = $bmp.Height
$cx = $w / 2.0
$cy = $h / 2.0
$innerR = [Math]::Min($w, $h) * $InnerRadiusRatio
$out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $c = $bmp.GetPixel($x, $y)
        $dx = $x - $cx
        $dy = $y - $cy
        $dist = [Math]::Sqrt($dx * $dx + $dy * $dy)
        $lum = ($c.R + $c.G + $c.B) / 3.0
        if ($dist -lt $innerR) {
            $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        }
        elseif ($lum -lt 28 -and $c.G -lt 40) {
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
$out.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$out.Dispose()
Write-Host "Saved $dst"
