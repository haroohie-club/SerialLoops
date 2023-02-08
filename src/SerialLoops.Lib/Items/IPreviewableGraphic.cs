using System;
using SkiaSharp;

namespace SerialLoops.Lib.Items
{

    public interface IPreviewableGraphic
    {
        protected SKBitmap GetPreview(Project project);
        
        public SKBitmap GetPreview(Project project, int maxWidth, int maxHeight)
        {
            var preview = GetPreview(project);
            var scale = Math.Min(maxWidth / (float)preview.Width, maxHeight / (float)preview.Height);
            return preview.Resize(new SKImageInfo((int)(preview.Width * scale), (int)(preview.Height * scale)), SKFilterQuality.None);
        }
    }
    
    public class NonePreviewableGraphic : ItemDescription, IPreviewableGraphic
    {
        public static NonePreviewableGraphic BACKGROUND = new(ItemType.Background);
        public static NonePreviewableGraphic CHARACTER_SPRITE = new(ItemType.Character_Sprite);

        private NonePreviewableGraphic(ItemType type) : base("NONE", type) { }

        public SKBitmap GetPreview(Project project)
        {
            return new SKBitmap(64, 64);
        }
    }
}