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
        public static SKColor ToSKColor(this Color color)
        {
            return new SKColor((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
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
