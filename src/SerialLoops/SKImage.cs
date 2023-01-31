using Eto.Drawing;
using SkiaSharp;
using System.IO;

namespace SerialLoops
{
    public class SKImage : Bitmap
    {
        public SKBitmap skBitmap { get; set; }

        public SKImage(SKBitmap skBitmap) : base(skBitmap.Encode(SKEncodedImageFormat.Png, 1).AsStream())
        {
        }
    }
}
