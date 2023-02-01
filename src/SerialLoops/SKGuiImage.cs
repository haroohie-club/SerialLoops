using Eto.Drawing;
using SkiaSharp;

namespace SerialLoops
{
    public class SKGuiImage : Bitmap
    {
        public SKBitmap skBitmap { get; set; }

        public SKGuiImage(SKBitmap skBitmap) : base(skBitmap.Encode(SKEncodedImageFormat.Png, 1).AsStream())
        {
        }
    }
}
