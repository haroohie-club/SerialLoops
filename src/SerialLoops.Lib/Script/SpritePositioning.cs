using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Lib.Script
{
    public class SpritePositioning
    {
        public int X { get; set; }
        public int Layer { get; set; }

        public SKPoint GetSpritePosition(SKBitmap sprite)
        {
            return new SKPoint(X, 384 - sprite.Height);
        }

        public enum SpritePosition
        {
            LEFT,
            CENTER,
            RIGHT,
        }
    }

    public struct PositionedSprite
    {
        public CharacterSpriteItem Sprite { get; set; }
        public SpritePositioning Positioning { get; set; }
        public SKPaint PalEffect { get; set; }
    }
}
