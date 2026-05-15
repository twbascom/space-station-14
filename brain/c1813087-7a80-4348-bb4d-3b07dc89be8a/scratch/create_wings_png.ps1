Add-Type -AssemblyName System.Drawing
$path = 'C:\space-station-14\Resources\Textures\Mobs\Customization\elika_wings.rsi'
if (!(Test-Path $path)) { New-Item -ItemType Directory -Path $path -Force }
$target = Join-Path $path "wings.png"
$bmp = New-Object System.Drawing.Bitmap 32,128
$bmp.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
