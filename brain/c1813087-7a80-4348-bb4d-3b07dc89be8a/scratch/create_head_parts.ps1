Add-Type -AssemblyName System.Drawing
$basePath = 'C:\space-station-14\Resources\Textures\Mobs\Customization'

# Create Floof RSI
$floofPath = Join-Path $basePath 'elika_floof.rsi'
if (!(Test-Path $floofPath)) { New-Item -ItemType Directory -Path $floofPath -Force }
$bmp = New-Object System.Drawing.Bitmap 32,128
$bmp.Save((Join-Path $floofPath 'floof.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# Create Ears RSI
$earsPath = Join-Path $basePath 'elika_ears.rsi'
if (!(Test-Path $earsPath)) { New-Item -ItemType Directory -Path $earsPath -Force }
$bmp2 = New-Object System.Drawing.Bitmap 32,128
$bmp2.Save((Join-Path $earsPath 'ears.png'), [System.Drawing.Imaging.ImageFormat]::Png)

$bmp.Dispose()
$bmp2.Dispose()
