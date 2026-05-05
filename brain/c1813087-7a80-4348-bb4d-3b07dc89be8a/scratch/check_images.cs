using System;
using SixLabors.ImageSharp;

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
                    using var image = Image.Load(file);
                    Console.WriteLine($"{file}: {image.Width}x{image.Height}");
                } catch (Exception e) {
                    Console.WriteLine($"{file}: Error {e.Message}");
                }
            }
        }
    }
}
