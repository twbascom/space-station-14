using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

var path = @"C:\space-station-14\Resources\Textures\Mobs\Customization\elika_wings.rsi";
Directory.CreateDirectory(path);
var target = Path.Combine(path, "wings.png");
using var bmp = new Bitmap(32, 32);
bmp.Save(target, ImageFormat.Png);
