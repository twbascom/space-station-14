Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("C:\space-station-14\Resources\Textures\Mobs\Customization\elika_wings.rsi\wings.png")
Write-Output "Dimensions: $($img.Width)x$($img.Height)"
$img.Dispose()
