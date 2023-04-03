using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Lib.Script
{
    public class SpritePositioning
    {
        public SpritePosition Position { get; set; }
        public int Layer { get; set; }

        public SKPoint GetSpritePosition(SKBitmap sprite)
        {
            switch (Position)
            {
                case SpritePosition.LEFT:
                    return new SKPoint(-50, 384 - sprite.Height);

                default:
                case SpritePosition.CENTER:
                    return new SKPoint(0, 384 - sprite.Height);

                case SpritePosition.RIGHT:
                    return new SKPoint(50, 384 - sprite.Height);
            }
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
