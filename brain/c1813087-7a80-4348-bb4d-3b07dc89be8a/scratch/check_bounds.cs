using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = {
                @"c:\space-station-14\Resources\Textures\Mobs\Species\Namiod\parts.rsi\tailfrontwagging.png",
                @"c:\space-station-14\Resources\Textures\Mobs\Species\Namiod\parts.rsi\tailbackwag.png",
                @"c:\space-station-14\Resources\Textures\Mobs\Species\Namiod\parts.rsi\tailsidewag.png"
            };

            foreach (var file in files)
            {
                try {
                    using var image = Image.Load<Rgba32>(file);
                    int minX = image.Width, minY = image.Height, maxX = 0, maxY = 0;
                    bool found = false;

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            if (image[x, y].A > 0)
                            {
                                if (x < minX) minX = x;
                                if (y < minY) minY = y;
                                if (x > maxX) maxX = x;
                                if (y > maxY) maxY = y;
                                found = true;
                            }
                        }
                    }

                    if (found)
                        Console.WriteLine($"{file}: Bounds ({minX},{minY}) to ({maxX},{maxY}) Size {maxX-minX+1}x{maxY-minY+1}");
                    else
                        Console.WriteLine($"{file}: Empty");
                } catch (Exception e) {
                    Console.WriteLine($"{file}: Error {e.Message}");
                }
            }
        }
    }
}
