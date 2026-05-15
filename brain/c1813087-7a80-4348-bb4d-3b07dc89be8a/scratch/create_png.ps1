Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 32,32
$bmp.Save('C:\space-station-14\Resources\Textures\Mobs\Customization\elika_wings.rsi\wings.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
