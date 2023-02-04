using Eto.Drawing;
using SkiaSharp;

namespace SerialLoops.Utility
{
    public static class SKGuiHelpers
    {
        public static Color ToEtoDrawingColor(this SKColor skColor)
        {
            return new Color(skColor.Red / 255.0f, skColor.Green / 255.0f, skColor.Blue / 255.0f);
        }
    }

    public class SKGuiImage : Bitmap
    {
        public SKBitmap SkBitmap { get; set; }

        public SKGuiImage(SKBitmap skBitmap) : base(skBitmap.Encode(SKEncodedImageFormat.Png, 1).AsStream())
        {
        }
    }
}
