using System.Collections.Generic;
using System.IO;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items
{
    public class ItemItem : Item, IPreviewableGraphic
    {
        public GraphicsFile ItemGraphic { get; set; }
        public int ItemIndex { get; set; }

        public ItemItem(string name) : base(name, ItemType.Item)
        {
        }
        public ItemItem(string name, int itemIndex, short grpIndex, Project project) : base(name, ItemType.Item)
        {
            ItemGraphic = project.Grp.GetFileByIndex(grpIndex);
            ItemIndex = itemIndex;
        }

        public SKBitmap GetImage()
        {
            return ItemGraphic.GetImage(transparentIndex: 0);
        }

        public void SetImage(SKBitmap image, IProgressTracker tracker, ILogger log)
        {
            tracker.Focus("Setting item image...", 1);
            List<SKColor> palette = Helpers.GetPaletteFromImage(image, 255, log);
            ItemGraphic.SetPalette(palette, 0);
            ItemGraphic.SetImage(image);
            tracker.Finished++;
        }

        public void Write(Project project, ILogger log)
        {
            using MemoryStream grp1Stream = new();
            ItemGraphic.GetImage().Encode(grp1Stream, SKEncodedImageFormat.Png, 1);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{ItemGraphic.Index:X3}.png"), grp1Stream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{ItemGraphic.Index:X3}.gi"), ItemGraphic.GetGraphicInfoFile(), project, log);
        }

        SKBitmap IPreviewableGraphic.GetPreview(Project project)
        {
            return GetImage();
        }

        public override void Refresh(Project project, ILogger log)
        {
        }

        public enum ItemLocation
        {
            Exit = 221,
            Left = 222,
            Center = 223,
            Right = 224,
        }

        public enum ItemTransition
        {
            Slide = 225,
            Fade = 226,
        }
    }
}
