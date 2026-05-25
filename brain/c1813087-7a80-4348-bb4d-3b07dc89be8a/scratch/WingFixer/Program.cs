using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WingFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            string customizationPath = @"C:\space-station-14\Resources\Textures\Mobs\Customization";
            foreach (var dir in Directory.GetDirectories(customizationPath, "elika_*"))
            {
                foreach (var file in Directory.GetFiles(dir, "*.png"))
                {
                    FixImage(file);
                }
            }

            string partsPath = @"C:\space-station-14\Resources\Textures\Mobs\Species\Elika\parts.rsi";
            foreach (var file in Directory.GetFiles(partsPath, "*.png"))
            {
                FixImage(file);
            }
        }

        static void FixImage(string path)
        {
            if (!File.Exists(path)) return;

            using (var image = Image.Load<Rgba32>(path))
            {
                Console.WriteLine($"{Path.GetFileName(path)}: {image.Width}x{image.Height}");

                if (image.Width == 32 && image.Height == 128)
                {
                    Console.WriteLine($"Fixing {Path.GetFileName(path)} (1x4 -> 2x2)...");
                    using (var newImage = new Image<Rgba32>(64, 64))
                    {
                        // S -> TL (0,0)
                        CopyRect(image, newImage, 0, 0, 0, 0);
                        // N -> TR (32,0)
                        CopyRect(image, newImage, 0, 32, 32, 0);
                        // E -> BL (0,32)
                        CopyRect(image, newImage, 0, 64, 0, 32);
                        // W -> BR (32,32)
                        CopyRect(image, newImage, 0, 96, 32, 32);

                        newImage.Save(path);
                    }
                    Console.WriteLine("Done.");
                }
            }
        }

        static void CopyRect(Image<Rgba32> source, Image<Rgba32> dest, int srcX, int srcY, int destX, int destY)
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    dest[destX + x, destY + y] = source[srcX + x, srcY + y];
                }
            }
        }
    }
}
