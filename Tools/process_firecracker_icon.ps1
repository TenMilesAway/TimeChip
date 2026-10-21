Add-Type -AssemblyName System.Drawing
$src = "C:\Users\zll\.cursor\projects\d-Unity-Project-TimeChip-TimeChip\assets\firecrackers_150_full.jpg"
$dst = "D:\Unity Project\TimeChip\TimeChip\Assets\Art\LittleGame\Sprites\SpringFestival\firecrackers.png"
$tmp = "D:\Unity Project\TimeChip\TimeChip\Assets\Art\LittleGame\Sprites\SpringFestival\firecrackers_tmp.png"
$srcBmp = [System.Drawing.Bitmap]::FromFile($src)
$w = $srcBmp.Width
$h = $srcBmp.Height
$work = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($work)
$g.DrawImage($srcBmp, 0, 0, $w, $h)
$g.Dispose()
$srcBmp.Dispose()

$minX = $w
$minY = $h
$maxX = 0
$maxY = 0
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $c = $work.GetPixel($x, $y)
        if ($c.A -lt 10) { continue }
        if ($c.R -lt 35 -and $c.G -lt 35 -and $c.B -lt 35) { continue }
        if ($x -lt $minX) { $minX = $x }
        if ($y -lt $minY) { $minY = $y }
        if ($x -gt $maxX) { $maxX = $x }
        if ($y -gt $maxY) { $maxY = $y }
    }
}

$cw = $maxX - $minX + 1
$ch = $maxY - $minY + 1
$crop = New-Object System.Drawing.Bitmap $cw, $ch, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gc = [System.Drawing.Graphics]::FromImage($crop)
$srcRect = New-Object System.Drawing.Rectangle $minX, $minY, $cw, $ch
$destRect = New-Object System.Drawing.Rectangle 0, 0, $cw, $ch
$gc.DrawImage($work, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
$gc.Dispose()
$work.Dispose()

# Cover 150x150: scale up until both dimensions fill the canvas, then center-crop.
$scale = [Math]::Max(150.0 / $cw, 150.0 / $ch)
$nw = [int][Math]::Round($cw * $scale)
$nh = [int][Math]::Round($ch * $scale)
$scaled = New-Object System.Drawing.Bitmap $nw, $nh, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gs = [System.Drawing.Graphics]::FromImage($scaled)
$gs.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gs.DrawImage($crop, 0, 0, $nw, $nh)
$gs.Dispose()
$crop.Dispose()

$out = New-Object System.Drawing.Bitmap 150, 150, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$go = [System.Drawing.Graphics]::FromImage($out)
$go.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
$go.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$ox = [int](($nw - 150) / 2)
$oy = [int](($nh - 150) / 2)
$srcRect = New-Object System.Drawing.Rectangle $ox, $oy, 150, 150
$destRect = New-Object System.Drawing.Rectangle 0, 0, 150, 150
$go.DrawImage($scaled, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
$scaled.Dispose()
$go.Dispose()
$out.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
$out.Dispose()
Move-Item -Force $tmp $dst
Write-Output "Saved $dst (${nw}x${nh} content in 150x150)"
