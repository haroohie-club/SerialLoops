using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class LayoutItem(GraphicsFile layoutFile, List<GraphicsFile> grps, int startEntry, int numEntries, string name) : Item(name, ItemType.Layout), IPreviewableGraphic
    {
        public GraphicsFile Layout { get; set; } = layoutFile;
        public List<GraphicsFile> GraphicsFiles { get; set; } = grps;
        public int StartEntry { get; set; } = startEntry;
        public int NumEntries { get; set; } = numEntries;

        public override void Refresh(Project project, ILogger log)
        {
        }

        public SKBitmap GetLayoutImage()
        {
            return Layout.GetLayout(GraphicsFiles, Layout.LayoutEntries.Skip(StartEntry).Take(NumEntries).ToList(), darkMode: false, preprocessedList: true).bitmap;
        }

        SKBitmap IPreviewableGraphic.GetPreview(Project project)
        {
            return GetLayoutImage();
        }
    }
}
