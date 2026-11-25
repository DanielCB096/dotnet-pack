param(
    [string]$SourcePath = "docs/capturas/pipelinekit-dashboard.png",
    [string]$OutputPath = "docs/capturas/pipelinekit-storyboard.png"
)

Add-Type -AssemblyName System.Drawing

$inputFile = Join-Path (Get-Location) $SourcePath
$outputFile = Join-Path (Get-Location) $OutputPath
$img = [System.Drawing.Image]::FromFile($inputFile)

$canvasWidth = 1120
$canvasHeight = 880
$panelWidth = 500
$left = 40
$right = $left + $panelWidth + 40
$top = 110

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
$graphics.DrawString("demo walkthrough - dashboard, metrics and logs", $labelFont, $muted, 42, 72)

function Draw-Panel {
    param(
        [System.Drawing.Rectangle]$Crop,
        [int]$X,
        [int]$Y,
        [string]$Label
    )

    $scale = $panelWidth / $Crop.Width
    $height = [int]($Crop.Height * $scale)
    $destination = New-Object System.Drawing.Rectangle $X, $Y, $panelWidth, $height
    $graphics.DrawImage($img, $destination, $Crop, [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.DrawString($Label, $labelFont, $white, $X, $Y + $height + 12)
    return $height
}

$full = [System.Drawing.Rectangle]::new(0, 0, $img.Width, $img.Height)
$metrics = [System.Drawing.Rectangle]::new(0, [int]($img.Height * 0.10), $img.Width, [int]($img.Height * 0.46))
$logsTop = [int]($img.Height * 0.57)
$logs = [System.Drawing.Rectangle]::new(0, $logsTop, $img.Width, $img.Height - $logsTop)

Draw-Panel -Crop $full -X $left -Y $top -Label "1. Dashboard" | Out-Null

$metricsHeight = Draw-Panel -Crop $metrics -X $right -Y $top -Label "2. Metrics"
$logsY = $top + $metricsHeight + 50
Draw-Panel -Crop $logs -X $right -Y $logsY -Label "3. Logs" | Out-Null

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
