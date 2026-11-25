param(
    [string]$SourcePath = "docs/capturas/pipelinekit-dashboard.png",
    [string]$OutputPath = "docs/capturas/pipelinekit-storyboard.png"
)

Add-Type -AssemblyName System.Drawing

$inputFile = Join-Path (Get-Location) $SourcePath
$outputFile = Join-Path (Get-Location) $OutputPath
$img = [System.Drawing.Image]::FromFile($inputFile)

$canvasWidth = 1600
$canvasHeight = 900
$panelWidth = 500
$panelHeight = 715
$top = 105
$gap = 28

$canvas = New-Object System.Drawing.Bitmap $canvasWidth, $canvasHeight
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.Clear([System.Drawing.Color]::FromArgb(15, 23, 42))

$titleFont = New-Object System.Drawing.Font("Segoe UI", 28, [System.Drawing.FontStyle]::Bold)
$labelFont = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Regular)
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(226, 232, 240))
$muted = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(148, 163, 184))
$accent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(56, 189, 248))

$graphics.DrawString("PipelineKit", $titleFont, $accent, 40, 25)
$graphics.DrawString("demo walkthrough - dashboard, metrics and logs", $labelFont, $muted, 42, 70)

$crops = @(
    [System.Drawing.Rectangle]::new(0, 0, $img.Width, $img.Height),
    [System.Drawing.Rectangle]::new(0, 0, $img.Width, [int]($img.Height * 0.55)),
    [System.Drawing.Rectangle]::new(0, [int]($img.Height * 0.55), $img.Width, [int]($img.Height * 0.45))
)

$labels = @("1. Dashboard", "2. Metrics", "3. Logs")

for ($index = 0; $index -lt $crops.Count; $index++) {
    $x = 40 + $index * ($panelWidth + $gap)
    $destination = New-Object System.Drawing.Rectangle $x, $top, $panelWidth, $panelHeight
    $graphics.DrawImage($img, $destination, $crops[$index], [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.DrawString($labels[$index], $labelFont, $white, $x, $top + $panelHeight + 12)
}

$graphics.Dispose()
$canvas.Save($outputFile, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
$img.Dispose()
$titleFont.Dispose()
$labelFont.Dispose()
$white.Dispose()
$muted.Dispose()
$accent.Dispose()

Get-Item $outputFile | Select-Object FullName, Length
