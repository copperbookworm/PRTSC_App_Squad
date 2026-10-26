$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 64.0
    $bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,24,26,30))
    $g.FillEllipse($bg, 1*$s, 1*$s, 62*$s, 62*$s)
    $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,88,166,255)), (5*$s)
    $g.DrawEllipse($ring, 15*$s, 17*$s, 34*$s, 34*$s)
    $lens = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,88,166,255))
    $g.FillEllipse($lens, 25*$s, 27*$s, 14*$s, 14*$s)
    $notch = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,88,166,255))
    $g.FillRectangle($notch, 25*$s, 8*$s, 14*$s, 6*$s)
    $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($size in $sizes) {
    $bmp = New-IconBitmap $size
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngs += ,($ms.ToArray())
    $ms.Dispose()
}

$out = "D:\mil-ai\PRTSC_app\app.ico"
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0)      # reserved
$bw.Write([uint16]1)      # type = icon
$bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $b = if ($size -ge 256) { 0 } else { $size }
    $bw.Write([byte]$b)
    $bw.Write([byte]$b)
    $bw.Write([byte]0)    # color count
    $bw.Write([byte]0)    # reserved
    $bw.Write([uint16]1)  # planes
    $bw.Write([uint16]32) # bit count
    $bw.Write([uint32]$pngs[$i].Length)
    $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($png in $pngs) { $bw.Write($png) }
$bw.Flush(); $bw.Dispose(); $fs.Dispose()
"ico bytes: $((Get-Item $out).Length)"
