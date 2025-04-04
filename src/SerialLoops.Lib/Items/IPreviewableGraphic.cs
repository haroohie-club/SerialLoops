using System;
using SkiaSharp;

namespace SerialLoops.Lib.Items;

public interface IPreviewableGraphic
{
    public string Text => ((ItemDescription)this).DisplayName;

    protected SKBitmap GetPreview(Project project);

    public SKBitmap GetPreview(Project project, int maxWidth, int maxHeight)
    {
        SKBitmap preview = GetPreview(project);
        float scale = Math.Min(maxWidth / (float)preview.Width, maxHeight / (float)preview.Height);
        return preview.Resize(new SKImageInfo((int)(preview.Width * scale), (int)(preview.Height * scale)), SKSamplingOptions.Default);
    }
}

public class NonePreviewableGraphic : ItemDescription, IPreviewableGraphic
{
    public static readonly NonePreviewableGraphic BACKGROUND = new(ItemType.Background);
    public static readonly NonePreviewableGraphic CHARACTER_SPRITE = new(ItemType.Character_Sprite);
    public static readonly NonePreviewableGraphic PLACE = new(ItemType.Place);

    private NonePreviewableGraphic(ItemType type) : base("NONE", type, "NONE") { }

    public SKBitmap GetPreview(Project project)
    {
        return new(64, 64);
    }
}
