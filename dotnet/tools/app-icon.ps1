# Draws the app icon (a concrete probeta after a cone-and-cone failure) and writes
# src/ProbetaControl/Assets/app.ico (16, 24, 32, 48, 64, 256 px) and app.png (256 px).
# Vector drawing on a 256-unit canvas with WPF; small sizes get thicker lines so the shape stays readable.
# Run with:  powershell -NoProfile -STA -File dotnet\tools\app-icon.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$assets = Join-Path $PSScriptRoot '..\src\ProbetaControl\Assets'

function Color([string]$hex) { [System.Windows.Media.ColorConverter]::ConvertFromString($hex) }
function Brush([string]$hex) { $b = New-Object System.Windows.Media.SolidColorBrush (Color $hex); $b.Freeze(); $b }
function Pen($hex, [double]$width) {
    $p = New-Object System.Windows.Media.Pen (Brush $hex), $width
    $p.LineJoin = 'Round'; $p.StartLineCap = 'Round'; $p.EndLineCap = 'Round'; $p.Freeze(); $p
}
function Geo([string]$data) { [System.Windows.Media.Geometry]::Parse($data) }
function Placed($geometry) {
    # Whole drawing: scale 0.96, offset (5, 2) to fit the canvas with the fragments.
    $g = $geometry.Clone()
    $t = New-Object System.Windows.Media.TransformGroup
    $t.Children.Add((New-Object System.Windows.Media.ScaleTransform 0.96, 0.96)) | Out-Null
    $t.Children.Add((New-Object System.Windows.Media.TranslateTransform 5, 2)) | Out-Null
    $g.Transform = $t; $g
}
function Shape($group, $brush, $pen, $geometry) {
    $group.Children.Add((New-Object System.Windows.Media.GeometryDrawing $brush, $pen, (Placed $geometry))) | Out-Null
}
function Ellipse([double]$cx, [double]$cy, [double]$rx, [double]$ry) {
    New-Object System.Windows.Media.EllipseGeometry (New-Object System.Windows.Point $cx, $cy), $rx, $ry
}

# $f thickens the lines at small sizes; $texture adds aggregate dots and the outline of the lost sides (48 px and up).
function Draw([double]$f, [bool]$texture) {
    $g = New-Object System.Windows.Media.DrawingGroup
    $outline = '#353A40'
    $concrete = New-Object System.Windows.Media.LinearGradientBrush (Color '#CDD0D3'), (Color '#9EA3A8'), 0
    $concrete.Freeze()
    $fragment = Brush '#868C92'

    # Cylinder grid: x 56..200, cap centre y 40, base y 216 (rx 72, ry 24). The sides break off in cones.
    $left = 'L 68,70 L 64,84 L 82,92 L 78,106 L 98,114 L 108,128 L 98,140 L 84,146 L 88,162 L 70,170 L 74,184 L 58,196'
    $right = 'L 198,196 L 182,184 L 186,170 L 168,162 L 172,146 L 158,140 L 148,128 L 158,114 L 178,106 L 174,92 L 192,84 L 188,70 L 200,60'
    $hourglass = Geo "M 56,40 L 56,60 $left L 56,216 A 72,24 0 0 0 200,216 $right L 200,40 A 72,24 0 0 0 56,40 Z"

    if ($texture) {
        # Faint outline of the original cylinder: what broke away.
        $ghost = New-Object System.Windows.Media.Pen (Brush '#B7BDC4'), 4
        $ghost.DashStyle = New-Object System.Windows.Media.DashStyle -ArgumentList ([double[]](2, 3)), 0.0
        $ghost.Freeze()
        Shape $g $null $ghost (Geo 'M 56,60 L 56,196 M 200,60 L 200,196')
    }

    # The spalled pieces lying beside it.
    foreach ($p in 'M 4,222 L 18,186 L 40,190 L 50,214 L 28,232 Z', 'M 252,222 L 238,186 L 216,190 L 206,214 L 228,232 Z') {
        Shape $g $fragment (Pen $outline (6 * $f)) (Geo $p)
    }

    Shape $g $concrete (Pen $outline (8 * $f)) $hourglass
    if ($texture) {
        $dots = New-Object System.Windows.Media.DrawingGroup
        $dots.ClipGeometry = Placed $hourglass
        foreach ($a in @(@(76, 96, 5, '#7F858B'), @(98, 150, 4, '#E4E6E8'), @(170, 104, 4, '#7F858B'), @(150, 182, 5, '#E4E6E8'),
                         @(84, 196, 4, '#7F858B'), @(180, 168, 4, '#7F858B'), @(122, 120, 3, '#E4E6E8'), @(66, 132, 3, '#E4E6E8'))) {
            $dots.Children.Add((New-Object System.Windows.Media.GeometryDrawing (Brush $a[3]), $null, (Placed (Ellipse $a[0] $a[1] $a[2] $a[2])))) | Out-Null
        }
        $g.Children.Add($dots) | Out-Null
        Shape $g $null (Pen $outline (8 * $f)) $hourglass
    }

    Shape $g (Brush '#DDE0E2') (Pen $outline (8 * $f)) (Ellipse 128 40 72 24)
    $g
}

function Render([int]$size) {
    $f = switch ($size) { 16 { 2.2 } 24 { 1.8 } 32 { 1.5 } default { 1.0 } }
    $drawing = Draw $f ($size -ge 48)
    $dv = New-Object System.Windows.Media.DrawingVisual
    $dc = $dv.RenderOpen()
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 256.0), ($size / 256.0)))
    $dc.DrawDrawing($drawing)
    $dc.Pop(); $dc.Close()
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($dv)
    # Icons store straight (not premultiplied) alpha.
    $straight = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap $bmp, ([System.Windows.Media.PixelFormats]::Bgra32), $null, 0
    $straight.Freeze(); $straight
}

function PngBytes($bitmap) {
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $ms = New-Object IO.MemoryStream
    $enc.Save($ms); $ms.ToArray()
}

# Classic icon image: BITMAPINFOHEADER + bottom-up 32-bit BGRA rows + an all-zero AND mask (alpha decides).
function DibBytes($bitmap) {
    $size = $bitmap.PixelWidth
    $stride = $size * 4
    $pixels = New-Object byte[] ($stride * $size)
    $bitmap.CopyPixels($pixels, $stride, 0)
    $maskStride = [int][Math]::Ceiling($size / 32.0) * 4
    $ms = New-Object IO.MemoryStream
    $w = New-Object IO.BinaryWriter $ms
    $w.Write([int]40); $w.Write([int]$size); $w.Write([int]($size * 2)); $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]0); $w.Write([int]($stride * $size + $maskStride * $size)); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0)
    for ($row = $size - 1; $row -ge 0; $row--) { $w.Write($pixels, $row * $stride, $stride) }
    $w.Write((New-Object byte[] ($maskStride * $size)))
    $w.Flush(); $ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 256
$images = foreach ($s in $sizes) {
    $bmp = Render $s
    , @($s, $(if ($s -eq 256) { PngBytes $bmp } else { DibBytes $bmp }))
}

# ICO file: header, one directory entry per image, then the images.
$ico = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $ico
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $s = $img[0]; $data = $img[1]
    $w.Write([byte]($s % 256)); $w.Write([byte]($s % 256))     # 256 is written as 0
    $w.Write([byte]0); $w.Write([byte]0); $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]$data.Length); $w.Write([int]$offset)
    $offset += $data.Length
}
foreach ($img in $images) { $w.Write([byte[]]$img[1]) }
$w.Flush()
[IO.File]::WriteAllBytes((Join-Path $assets 'app.ico'), $ico.ToArray())
[IO.File]::WriteAllBytes((Join-Path $assets 'app.png'), (PngBytes (Render 256)))
"Wrote app.ico ($($sizes -join ', ') px) and app.png (256 px) in $((Resolve-Path $assets).Path)"
