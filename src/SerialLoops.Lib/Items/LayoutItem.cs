using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.Items
{
    public class LayoutItem(int layoutIndex, List<GraphicsFile> grps, int startEntry, int numEntries, string name, Project project) : Item(name, ItemType.Layout), IPreviewableGraphic
    {
        public GraphicsFile Layout { get; set; } = project.LayoutFiles[layoutIndex];
        public List<GraphicsFile> GraphicsFiles { get; set; } = grps;
        public int StartEntry { get; set; } = startEntry;
        public int NumEntries { get; set; } = numEntries;

        private readonly Dictionary<int, SKBitmap> _tilesDict = grps.Select((g, i) => (i, g.GetImage(transparentIndex: 0))).ToDictionary();

        public override void Refresh(Project project, ILogger log)
        {
        }

        public SKBitmap GetLayoutImage()
        {
            return Layout.GetLayout(GraphicsFiles, Layout.LayoutEntries.Skip(StartEntry).Take(NumEntries).ToList(), darkMode: false, preprocessedList: true).bitmap;
        }

        public (SKBitmap tile, SKRect dest) GetLayoutEntryRender(int index)
        {
            if (index < 0 || Layout.LayoutEntries[index].RelativeShtxIndex < 0)
            {
                return (null, new());
            }
            else
            {
                return (Layout.LayoutEntries[index].GetTileBitmap(_tilesDict), Layout.LayoutEntries[index].GetDestination());
            }
        }

        SKBitmap IPreviewableGraphic.GetPreview(Project project)
        {
            return GetLayoutImage();
        }
    }
}
