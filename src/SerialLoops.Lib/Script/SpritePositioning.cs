using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SkiaSharp;

namespace SerialLoops.Lib.Script;

public class SpritePositioning
{
    public int X { get; set; }
    public int Layer { get; set; }

    public SKPoint GetSpritePosition(SKBitmap sprite, int verticalOffset)
    {
        return new(X, 192 + verticalOffset - sprite.Height);
    }

    public enum SpritePosition
    {
        LEFT,
        CENTER,
        RIGHT,
    }
}

public class PositionedSprite
{
    public CharacterSpriteItem Sprite { get; set; }
    public SpritePositioning Positioning { get; set; }
    public int StartPosition { get; set; }
    public SKPaint PalEffect { get; set; }
    public SpritePreTransitionScriptParameter.SpritePreTransition PreTransition { get; set; }
    public SpritePostTransitionScriptParameter.SpritePostTransition PostTransition { get; set; }
}

public record PositionedChibi(ChibiItem Chibi, int X, int Y);
